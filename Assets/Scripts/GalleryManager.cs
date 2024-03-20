using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// This script acts as a controller for all targets in the gallery
// Possible modifications: Spawn in targets and despawn them instead
// of having a fixed array of targets that are randomly picked from

public class GalleryManager : NetworkBehaviour
{
	public NetworkVariable<bool> IsGalleryRunning = new NetworkVariable<bool>(false);
	[SerializeField] private float DelayBetweenTargets = 5.0f;
	
	// I originally had a way for this script to find the controller on its own on
	// spawning in, but Unity's tags suck and this is a simpler way. Possible future improvment?
	[SerializeField] private TargetStruck Controller = null;
	[SerializeField] private TargetStruck[] Targets;
	
	private int TargetsRemaining = 0;
	private int[] ShuffledIndexes = null;
	
	[SerializeField] private AudioSource GalleryAudioSource;
	[SerializeField] private AudioClip GalleryStartSFX; // For when the gallery starts
	[SerializeField] private AudioClip GalleryStopSFX; 	// For when the gallery is prematurely stopped
	[SerializeField] private AudioClip GalleryEndSFX; 	// For when the gallery has run out of targets
	
	// This is a C# list that holds a list of scores for each player, each UID is the list index
	// A player would access their score using ScoreList[OwnerClientID] or something similar
	// Each time a player fires their weapon, that script sends their UID and current points to
	// this script. This script then which player has the most points, and returns that value,
	// their UID, and the number of players, to the player's script, which then updates UI.
	private List<int> ScoreList = new List<int>();
	// To be quite honest I'm not very proud of how this works, it 100% could be better but I'm in a time crunch
	
	private List<UIManager> UIList = new List<UIManager>();
	
	// Update each player's UI with the correct best player and their score
	void Update()
	{
		ulong TempID = 0;
		int TempScore = 0;
		int NumPlayers = GetBestPlayer(ref TempID, ref TempScore); 
		
		foreach (UIManager UI in UIList)
		{
			UI.RefreshScore(NumPlayers, TempID, TempScore);
		}
	}
	
    // Externally called function (from the controller target) to begin the shooting gallery
    public void ToggleGallery()
    {
		IsGalleryRunning.Value = !IsGalleryRunning.Value;
		
		if (IsGalleryRunning.Value) // If the gallery is inactive, start it up
		{
			//Debug.Log("Starting gallery");
			GalleryAudioSource.PlayOneShot(GalleryStartSFX); // Play starting audio
			
			// Get a randomized array of indexes
			ShuffledIndexes = CreateShuffledIndexes(Targets.Length);
			
			// Start each target with a delay
			for (int i = 0; i < Targets.Length; i++) Targets[ShuffledIndexes[i]].Invoke("ToggleTarget", (DelayBetweenTargets * i));
		}
		else // Otherwise, make it stop
		{
			//Debug.Log("Stopping gallery");
			// Play audio depending on whether the gallery was prematurely stopped or ended naturally
			GalleryAudioSource.PlayOneShot(((TargetsRemaining <= 0) ? GalleryEndSFX : GalleryStopSFX));
			
			// Go through all active targets, deactivate them and cancel Invoke() calls
			foreach (TargetStruck i in Targets)
			{
				i.CancelInvoke("ToggleTarget");
				if (i.IsAvailable) i.ToggleTarget();
			}
		}
		
		TargetsRemaining = Targets.Length; // Must be reset AFTER everything else
    }
	
	// Function to create a shuffled array of indexes to be used as accessing TargetStruck[] in random order
	private int[] CreateShuffledIndexes(int ArrayLength)
	{
		var ShuffledArray = new int[ArrayLength];
		
		// Initialize each element to be equal to its index
		for (int i = 0; i < ShuffledArray.Length; i++) ShuffledArray[i] = i;
		
		// Shuffle the array elements
		for (int i = 0; i < ShuffledArray.Length; i++)
		{
			int RandomIndex = UnityEngine.Random.Range(0, ShuffledArray.Length); // Get a randomly chosen index
			
			// Swap the current (ith) element with the random one
			int temp = ShuffledArray[i];
			ShuffledArray[i] = ShuffledArray[RandomIndex];
			ShuffledArray[RandomIndex] = temp;
			// Not a particularly smart shuffler but it gets the job done
		}
		
		// Now we are returning an array with elements in the range [0,Length) with no duplicates
		return ShuffledArray;
	}
	
	// Externally called function (by regular targets when they are hit) to decrement the number of targets remaining
	public void DecrementTargets()
	{
		TargetsRemaining -= 1;
		
		// If we have ran out of targets while the gallery is running
		if ((TargetsRemaining <= 0) && IsGalleryRunning.Value)
		{
			if (Controller.IsAvailable) // If the gallery wasn't turned off by a player somehow
			{
				// Turn it all off
				Controller.ToggleTarget();
				ToggleGallery();
			}
		}
	}
	
	// Externally called function (by the players when they log in) to reserve some space for scores
	public void AddNewPlayer(UIManager UI) //(ulong NewPlayerID)
	{
		// Push new elements to both lists, the player's UID should line up with the index of their entry
		UIList.Add(UI);
		ScoreList.Add(0); // Create a new element
	}
	
	// A very cruddy function that puts the best player's ID and their score in the inputs, returns num of players
	// Could be significantly better but I was fixing this part near the deadline :)
	private int GetBestPlayer(ref ulong PlayerID, ref int Score)
	{
		PlayerID = 0;
		Score = 0;
		
		for (int i = 0; i < ScoreList.Count; i++)
		{
			if (ScoreList[i] > Score)
			{
				Score = ScoreList[i];
				PlayerID = (ulong)(i);
			}
		}
		
		return ScoreList.Count;
	}
	
	// Externally called function (by the UI manager) to update a player's score
	public int UpdateScore(ref ulong PlayerID, ref int Score) // Inputs are references so they can be used as output
	{
		if ((int)(PlayerID) > ScoreList.Count) // Prevent out of bounds exception if something goes wrong
		{
			Debug.Log("ERROR! Player ID " + PlayerID + " is out of bounds of expected number of players " + ScoreList.Count);
			return -1;
		}
		
		ScoreList[(int)(PlayerID)] = Score; // Save the value to the scoreboard
		
		// Find the current highest score and return it and its player
		// These two values are now being used as variables that will return info back to the plaer
		Score = 0;
		PlayerID = 0;
		
		for (int i = 0; i < ScoreList.Count; i++)
		{
			if (ScoreList[i] > Score)
			{
				Score = ScoreList[i];
				PlayerID = (ulong)(i);
			}
		}
		
		// Return value is the number of total players
		return ScoreList.Count;
	}
}
