using UnityEngine;

// This script handles playing SFX for the player's gun, using one of a few random sounds
// instead of the same one. To be clear, I do not intend each variation of a sound to be
// considered a unique sound effect as per the assignment instructions, I just wanted to
// add some flavor to the game SFX and learn how something like this is done.
// This was done with a lot of help from this: youtube.com/watch?v=t7OO24zF-lU
// All SFX for the "Garand" weapon are ripped straight from the video game Deep Rock Galactic

public class PlayGunshot : MonoBehaviour
{
	[SerializeField] private AudioSource GunAudioSource;
	[SerializeField] private AudioClip[] HipFireAudioArray;
	
    // Externally called by the player
    public void PlayAudio()
    {
        GunAudioSource.clip = HipFireAudioArray[Random.Range(0, HipFireAudioArray.Length)]; // Select a random sound from the array
		GunAudioSource.PlayOneShot(GunAudioSource.clip); // Play it
    }
}
