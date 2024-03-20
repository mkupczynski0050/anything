using UnityEngine;

// This script handles playing SFX for a target being shot by the player's gun, using one of a
// few random sounds instead of the same one. To be clear, I do not intend each variation of a
// sound to be considered a unique sound effect as per the assignment instructions, I just
// wanted to add some flavor to the game SFX and learn how something like this is done.
// This was done with a lot of help from this: youtube.com/watch?v=t7OO24zF-lU
// Pretty much all SFX at this point are ripped straight from the video game Deep Rock Galactic

public class PlayHit : MonoBehaviour
{
	[SerializeField] private AudioSource TargetAudioSource;
	[SerializeField] private AudioClip[] HitAudioArray;
	
    // Externally called by the gallery
    public void PlayAudio()
    {
        TargetAudioSource.clip = HitAudioArray[Random.Range(0, HitAudioArray.Length)]; // Select a random sound from the array
		TargetAudioSource.PlayOneShot(TargetAudioSource.clip); // Play it
    }
}
