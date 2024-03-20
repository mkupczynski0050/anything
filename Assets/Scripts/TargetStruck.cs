using UnityEngine;
using Unity.Netcode;

// This script handles a target getting damaged by a player
// The raycasting logic was largely done with help from this: youtube.com/watch?v=dXgb6J46yjk
// I don't think the name for this script is particularly accuracte to the functionality
// that it has expanded to, but at this point I'd rather leave it than risk breaking something

public class TargetStruck : NetworkBehaviour
{
	public NetworkVariable<Vector3> Position = new NetworkVariable<Vector3>();
	
	public bool IsAvailable = false;
	[SerializeField] private int DefaultHealth = 1;
	public int Health;
	[SerializeField] private int PointsPerTarget = 100;
	
	[SerializeField] private AudioSource TargetAudioSource;
	[SerializeField] private AudioClip TargetTurningSFX;
	private PlayHit HitAudioPlayer = null; // Only used by normal targets
	
	// Reference to the gallery manager
	private GalleryManager GalleryController;
	
	// If I ever need to change this, having it in one place like this helps reduce error
	private const string ControllerTag = "ControllerTarget";
	
	// On spawning in, get the gallery manager script and the audio scripts
	public override void OnNetworkSpawn()
	{
		GalleryController = GetComponentInParent<GalleryManager>();
		HitAudioPlayer = GetComponent<PlayHit>();
		
		// Check if this is the gallery control target
		if (transform.gameObject.CompareTag(ControllerTag))
		{
			// Make HP and point values unreasonable (they should never come into use anyway)
			DefaultHealth = int.MaxValue;
			PointsPerTarget = 0;
			
			// The boolean is now instead used as a controller for if the gallery is running
			IsAvailable = GalleryController.IsGalleryRunning.Value;
		}
		
		Health = DefaultHealth; // Reset HP
	}
	
	// When this target is hit by a player's hitscan
    public int Damage(int DamageInput, ulong ShooterID)
	{
		
		if (transform.gameObject.CompareTag(ControllerTag)) // If this is the gallery controller target
		{
			//Debug.Log("User " + ShooterID + " has " + (IsAvailable ? "stopped" : "started") + " the shooting gallery");
			
			// Toggle the booleans and start the gallery
			ToggleTarget();
			GalleryController.ToggleGallery();
		}
		else if (IsAvailable) // Otherwise, this is a regular target, check if it is active
		{
			Health -= DamageInput; // Reduce HP (In case I want to have targets that need multiple hits)
			HitAudioPlayer.PlayAudio(); // Epic sound effect
			//Debug.Log("Target has been struck by " + ShooterID);
			
			if (Health <= 0) // If the target has run out of HP, turn it around, deactivate it, and return some points
			{
				ToggleTarget();
				IsAvailable = false; // Just in case something is wrong, ENSURE this is false
				
				GalleryController.DecrementTargets(); // Tell the controller a target is down
				
				//Debug.Log("Giving user " + ShooterID + " " + PointsPerTarget + " points");
				return PointsPerTarget;
			}
		}
		
		return 0; // If the target either still has hp (thus didn't die) or is the controller target (can't be killed)
	}
	
	// Function to make this "inactive" target "active" (or vice versa) by making it turn around (which should make it face the players)
	public void ToggleTarget()
	{
		IsAvailable = !IsAvailable;
		MoveSelf();
		Health = DefaultHealth; // Reset HP
	}
	
	// Function to rotate an object 180* about the y axis (it is already tilted, so actually z axis)
	private void MoveSelf()
	{
		transform.Rotate(0.0f, 0.0f, 180.0f, Space.Self); // Rotate
		TargetAudioSource.PlayOneShot(TargetTurningSFX);  // Play turning audio
		Position.Value = transform.position; // Update network position
	}
}
