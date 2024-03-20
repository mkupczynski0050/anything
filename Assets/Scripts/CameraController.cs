using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

// This script handles camera movement, which is necessary because each client has their own local camera

public class CameraController : NetworkBehaviour
{
    [SerializeField] private GameObject CameraHolder;
	[SerializeField] private Vector3 CameraOffset;
	
	private Camera PlayerCamera;
	private AudioListener PlayerAudio;
	
	// On loading in, get the player's camera
	public override void OnNetworkSpawn()
	{
		if (IsOwner) // Only do this for your own camera you grabby clients!
		{
			PlayerCamera = GetComponentInChildren<Camera>();
			PlayerAudio = GetComponentInChildren<AudioListener>();
			
			// To get this to work, all cameras (and audio listeners b/c Unity kept complaining)
			// are disabled by default, and then only a client's local camera is enabled for them.
			// In the player prefab, we have an object called CameraHolder that acts as a unified
			// parent for the camera, and all objects on the player that should move along with the
			// camera (the arm and the gun). Rotation is imparted on the CameraHolder for movement.
			PlayerCamera.enabled = true;
			PlayerAudio.enabled = true;
		}
	}
	
	// Every frame, relocate the camera to the player's position
	void Update()
	{
		if(SceneManager.GetActiveScene().name == "Game")
		{
			CameraHolder.transform.position = transform.position + CameraOffset;
		}
	}
}
