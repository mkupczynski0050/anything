using UnityEngine;
using Unity.Netcode;

// This script handles player movement

public class OldPlayerMovement : NetworkBehaviour
{
	private Rigidbody PlayerBody;
	private Camera PlayerCamera;
	[SerializeField] private float PlayerSpeed = 1500f;
	[SerializeField] private float LookSensitivity = 20f;
	
	public NetworkVariable<Transform> Position = new NetworkVariable<Transform>();
	
	//public override void OnNetworkSpawn() // When a player loads in
	//{
	//	PlayerBody = GetComponent<Rigidbody>();
	//	PlayerCamera = GetComponentInChildren<Camera>();
	//	
	//	if (IsOwner) // If they are the owner, call this function to randomly displace spawn point
	//	{
	//		SpawnMove();
	//	}
	//}
	
	void Start()
	{
		PlayerBody = GetComponent<Rigidbody>();
		PlayerCamera = GetComponentInChildren<Camera>();
	}
	
	void FixedUpdate()
    {
		//if (!IsOwner) return; // Disallow other instances from controlling this player
		//if (!IsLocalPlayer) return; // Prevent other instances from affecting this player
		
		// Store WSAD input as a 3D vector for movement
		// Jumping isn't a thing in my game, but by doing this if I want to add it, not much needs to be changed
		Vector3 MoveVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
		
		// Store mouse input as a 3D vector for looking around
		Vector3 LookVector = new Vector3(Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"), 0f);
		
		// Send to the function FIXME: make different behavior based on host/client
		// youtube.com/watch?v=zSuR4B9hbkY, youtube.com/watch?v=kjUiARseG18
		Move(MoveVector, LookVector);
    }
	
	private void Move(Vector3 Movement, Vector3 Rotation)
	{
		// Force that represents what direction to move the player, relative to orientation, scaled by speed & delta time
		PlayerBody.AddRelativeForce(Movement * PlayerSpeed * Time.fixedDeltaTime);
		
		// The reasoning behind this is that we want to rotate the entire player about the Y axis
		// when they are looking left/right but not about the X axis when they are looking up/down.
		// That is instead handled by rotating the just the camera up/down instead of the whole player.
		Vector3 Yaw = new Vector3(-Rotation.x * LookSensitivity, 0f, 0f); // For looking up/down
		Vector3 Pitch = new Vector3(0f, Rotation.y * LookSensitivity, 0f); // For looking left/right
		
		// Impart rotation based on mouse input (this part is doing only left/right rotation)
		PlayerBody.MoveRotation(PlayerBody.rotation * Quaternion.Euler(Pitch));
		
		// Impart rotation based on mouse input (this part is doing only up/down rotation)
		PlayerCamera.transform.Rotate(Yaw);
	}
	
	//public void SpawnMove()
	//{
	//	if (NetworkManager.Singleton.IsServer) // If we are the server we can just immediately move
	//	{
	//		var randomPosition = GetRandomPositionOnPlane();
	//		transform.position = randomPosition;
	//		Position.Value = randomPosition;
	//	}
	//	else // If we aren't the server, we must send a request to the server to move us
	//	{
	//		SubmitPositionRequestServerRpc();
	//	}
	//}
	
	//[Rpc(SendTo.Server)] // Client asking the server to be moved
	//void SubmitPositionRequestServerRpc(RpcParams rpcParams = default)
	//{
	//	Position.Value = GetRandomPositionOnPlane();
	//}
	
	//static Vector3 GetRandomPositionOnPlane() // Generates a random Vector3 for spawning in players in a confined range
	//{
	//	return new Vector3(Random.Range(-3f, 3f), 1f, Random.Range(-3f, 3f));
	//}
	
	// Called by clients once per frame to observe movement handled by the server
	//void Update()
	//{
	//	transform.position = Position.Value;
	//}
}
