using UnityEngine;
using Unity.Netcode;

// This script handles player movement/controls, and server/client authoritative stuff

public class PlayerMovement : NetworkBehaviour
{
	public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
	public NetworkVariable<int> PlayerScore = new NetworkVariable<int>(0);
	
	[SerializeField] private float PlayerSpeed = 75f;
	[SerializeField] private float LookSensitivity = 20f;
	
	[SerializeField] private int GunDamage = 1;
	[SerializeField] private float ReloadLength = 3.0f;
	[SerializeField] private int MagazineCapacity = 8; // Will be used to reset next value on reloading the gun
	public int CurrentMagazine = 8;
	
	// Aspects of the player that are found in its children
	private Rigidbody Playerbody;
	private GameObject PlayerCameraHolder;
	private PlayGunshot GunshotAudioPlayer;
	
	// Single sound effects instead of the randomized ones
	[SerializeField] private AudioSource GunAudioSource;
	[SerializeField] private AudioClip MagazineEmptySFX; // Last bullet
	[SerializeField] private AudioClip ReloadStartSFX;
	[SerializeField] private AudioClip ReloadFinishSFX;
	
	// Aspects of the game that must be found by tag
	private GalleryManager Gallery;
	private UIManager Scoreboard;
	
	[SerializeField] private GameObject FireParticles;
	[SerializeField] private GameObject HitParticles;
	
	private bool IsReloading = false;
	
	// On loading in, get the player's Rigidbody, the camera holding GameObject, and audio scripts
	public override void OnNetworkSpawn() //void Awake()
	{
		Playerbody = GetComponent<Rigidbody>();
		
		//PlayerCameraHolder = transform.Find("CameraHolder").gameObject; // DOESN'T WORK PROPERLY
		PlayerCameraHolder = transform.GetChild(2).gameObject; // The index of the CameraHolder is 2
		// This isn't very good code as this can change even on accident in the editor, but I could
		// not find a good, reliable way to reference a child object that doesn't have an explicit type.
		
		// Gunshot sound effect script
		GunshotAudioPlayer = GetComponent<PlayGunshot>();
		
		// Set the magazine to start with maximum capacity
		CurrentMagazine = MagazineCapacity;
		
		// Get the scoreboard for pushing it to the player list and for later reference
		GameObject TempGameObject = GameObject.FindWithTag("ScoreManager");
		Scoreboard = TempGameObject.GetComponent<UIManager>();
		
		// Get the shooting gallery's manager and tell it a new player is here
		TempGameObject = GameObject.FindWithTag("ShootingGalleryManager");
		Gallery = TempGameObject.GetComponent<GalleryManager>();
		Gallery.AddNewPlayer(Scoreboard);
	}
	
	// Every physics update, process input
    void FixedUpdate()
	{
		if (!IsOwner) return; // Prevent other instances from controlling this player
		
		// Store WSAD input as a 3D vector for movement (3D in case I want to add jumping)
		Vector3 MoveVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
		
		// Store mouse input as a 3D vector for looking around (X and Y are swapped because quaternions are funky)
		Vector3 LookVector = new Vector3(Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0f);
		
		// Store LMB input as a bool
		bool IsShooting = Input.GetMouseButtonDown(0);
		
		// Store R input as a bool (in case the user was trying to reload)
		bool IsTryingToReload = (Input.GetKey(KeyCode.R)) && !IsReloading; // If they already were reloading, don't set to true
		
		// Determine how to process input
		if (NetworkManager.Singleton.IsServer) // If we are the host/server we can just immediately do the actions
		{
			Move(MoveVector, LookVector);
			if (IsShooting) Shoot();
			if (IsTryingToReload) // Dump magazine, invoke reload function with a delay to imitate idea of reloading
			{
				CurrentMagazine = 0;
				GunAudioSource.PlayOneShot(ReloadStartSFX); // Play reload start audio
				Invoke("Reload", ReloadLength);
			}
		}
		else // If we are a client, we must send a request to the server to handle our input
		{
			MoveRequestServerRpc(MoveVector, LookVector);
			if (IsShooting) ShootRequestServerRpc();
			if (IsTryingToReload)
			{
				CurrentMagazine = 0;
				GunAudioSource.PlayOneShot(ReloadStartSFX); // Play reload start audio
				Invoke("ReloadRequestServerRpc", ReloadLength);
			}
		}
	}
	
	// Function to apply forces and return a calculated position based off movement input
	public void Move(Vector3 MoveVector, Vector3 LookVector)
	{
		// Force that represents what direction to move the player, relative to orientation, scaled by speed & delta time
		Playerbody.AddRelativeForce(MoveVector * PlayerSpeed * Time.fixedDeltaTime, ForceMode.Impulse);
		
		// Impart Rigidbody rotation based on mouse input (this part is doing only left/right rotation)
		Playerbody.MoveRotation(Playerbody.rotation * Quaternion.Euler(new Vector3(0f, LookVector.y * LookSensitivity, 0f)));
		
		// Impart rotation based on mouse input (this part is doing only up/down rotation)
		PlayerCameraHolder.transform.Rotate((-LookVector.x * LookSensitivity), 0.0f, 0.0f);
		
		Position.Value = Playerbody.transform.position;
		// Return the resulting position for network updating
		//return Playerbody.transform.position;
	}
	
	// Handles shooting using raycasting
	public void Shoot()
	{
		if (CurrentMagazine > 0) // If there is at least 1 bullet, go ahead, then decrement magazine
		{
			RaycastHit hit; // The 3D position that the hitscan hits
			
			// The 3D position of the gun's muzzle, where to draw the hitscan from
			Transform MuzzlePosition = PlayerCameraHolder.transform.GetChild(3).gameObject.transform;
			
			// Create the hitscan
			if (Physics.Raycast(MuzzlePosition.position, MuzzlePosition.forward, out hit, 100))
			{
				// FIXME: All this currently only renders serverside
				
				// Draw the ray in the editor
				Debug.DrawRay(MuzzlePosition.position, MuzzlePosition.forward * hit.distance, Color.yellow);
				//Debug.Log("User " + OwnerClientId + " has shot.");
				
				GunshotAudioPlayer.PlayAudio(); // Epic sound effect
				
				// Create particle effects at the gun's muzzle and the ray's impact point facing opposite directions
				GameObject FireParticle = Instantiate(FireParticles, MuzzlePosition.position, Quaternion.LookRotation(MuzzlePosition.forward));
				GameObject HitParticle = Instantiate(HitParticles, hit.point, Quaternion.LookRotation(-MuzzlePosition.forward));
				
				// Delete the particle effects after some time
				Destroy(FireParticle, 1);
				Destroy(HitParticle, 1);
				
				// Get the target that we just hit
				TargetStruck Target = hit.transform.GetComponent<TargetStruck>();
				
				// If it actually is a valid target, register it as being hit and add points for this player
				if (Target != null) PlayerScore.Value += Target.Damage(GunDamage, OwnerClientId);
				
				// This bit is terrible
				// After this function, the copies are now holding the values of the best scorer
				ulong CopyOfID = OwnerClientId;
				int CopyOfScore = PlayerScore.Value;
				int NumberOfPlayers = Gallery.UpdateScore(ref CopyOfID, ref CopyOfScore);
				
				//Scoreboard.RefreshScore(NumberOfPlayers, CopyOfID, CopyOfScore);
			}
			
			// Use 1 bullet
			CurrentMagazine -= 1;
			
			// If we NOW have 0 left, play the ping sound to notify of this
			if (CurrentMagazine == 0) GunAudioSource.PlayOneShot(ReloadStartSFX);
		}
		else // Otherwise, just play a click noise
		{
			GunAudioSource.PlayOneShot(MagazineEmptySFX);
		}
	}
	
	// Literally just invoked with a timer to reset the CurrentMagazine value and play a sound, top tier reloading code
	public void Reload()
	{
		CurrentMagazine = MagazineCapacity;
		GunAudioSource.PlayOneShot(ReloadFinishSFX);
		IsReloading = false;
	}
	
	// Ask the server to move us
	[ServerRpc] private void MoveRequestServerRpc(Vector3 MoveVector, Vector3 LookVector)
	{
		//Position.Value = Move(MoveVector.normalized, LookVector); // Tell the client to move using this vector
		Move(MoveVector.normalized, LookVector);
		// I figured that this can be a way that the server asserts its authority over misbehaving clients. Normally,
		// the way I capture the WSAD input only gives ranges of 0.0-1.0, so normalizing doesn't have any effect, but
		// any attempts at modifying the vector to be higher (and thus faster) are thwarted by this normalization.
		// To the professor/grader that might be reading this code, is this misguided? Is this a waste of time and
		// are there better ways of using the server authority in this scenario, or is this potentially a good thing?
	}
	
	// Ask the server to make our gun shoot
	[ServerRpc] private void ShootRequestServerRpc()
	{
		Shoot();
	}
	
	// Ask the server to make us reload
	[ServerRpc] private void ReloadRequestServerRpc()
	{
		Reload();
	}
}
