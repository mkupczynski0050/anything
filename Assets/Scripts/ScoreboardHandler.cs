/*
using UnityEngine;
using Unity.Netcode;
using System;

public class ScoreboardHandler : MonoBehaviour
{
	// Have these events available
    public static event Action<GameObject> OnPlayerSpawn;
	public static event Action<GameObject> OnPlayerDespawn;
	
	// When the host loads in, this should become active
	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		
		OnPlayerSpawn?.Invoke(this.gameObject);
	}
	
	// When someone disconnects, 
	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		
		OnNetworkDespawn?.Invoke(this.gameObject);
	}
}
*/