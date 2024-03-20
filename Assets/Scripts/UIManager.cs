using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

// This script handles starting the game, and the buttons that provide this ability

public class UIManager : MonoBehaviour
{
	// Startup Buttons
	[SerializeField] private Button HostButton;
	[SerializeField] private Button ServerButton;
	[SerializeField] private Button ClientButton;
	
	// Persistent UI
	[SerializeField] private Text ScoreText;
	[SerializeField] private GalleryManager ScoreManager;
	
	// String segements that can be appended easily
	private string[] DefaultText = { "Number of Players: ", "Top Player: ", " - ", " Points" };
	
	void Awake()
	{
		// Set these actions for when each button is clicked
		HostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost(); EnterGame(); });
		ServerButton.onClick.AddListener(() => { NetworkManager.Singleton.StartServer(); EnterGame(); });
		ClientButton.onClick.AddListener(() => { NetworkManager.Singleton.StartClient(); EnterGame(); });
	}
	
	// This is called after any button is pressed. It "starts" the game by locking cursor and removing the buttons
	private void EnterGame()
	{
		// Set the cursor to be locked in the center of the screen since this is an FPS type game
		Cursor.lockState = CursorLockMode.Locked; 
		
		// Disable all buttons
		HostButton.gameObject.SetActive(false);
		ServerButton.gameObject.SetActive(false);
		ClientButton.gameObject.SetActive(false);
	}
	
	// This is called each frame by the gallery manager, it refreshes the UI displaying the highest scorer
	public void RefreshScore(int NumPlayers, ulong WinningID, int BestScore)
	{
		ScoreText.text = ""; // Clear
		
		// If this value was properly given, print it
		if (NumPlayers > 0) ScoreText.text += DefaultText[0] + NumPlayers + "\n";
		
		// Regardless, print the player with the highest score
		ScoreText.text += DefaultText[1] + WinningID + DefaultText[2] + BestScore + DefaultText[3];
	}
}
