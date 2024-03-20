using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// This file is left over from when I started this assignment with the help of the Hello World netcode tutorial
// After everything, it still works as intended, which is very cool (although it makes sense given how this works)

public class NetworkCommandLine : MonoBehaviour
{
	private NetworkManager netManager;
	
    // Start is called before the first frame update
    void Start()
    {
        netManager = GetComponentInParent<NetworkManager>();
		
		if (Application.isEditor) return; // Don't do anything if this is the editor (which cannot be from the CMD)
		
		var args = GetCommandLineArgs();
		
		if (args.TryGetValue("-mode", out string mode)) // Look for "-mode" in the input
		{
			switch (mode) // Start the appropriate netcode function
			{
				case "server":
					netManager.StartServer();
					break;
				case "host":
					netManager.StartHost();
					break;
				case "client":
					netManager.StartClient();
					break;
			}
		}
    }

    private Dictionary<string, string>GetCommandLineArgs()
	{
		Dictionary<string, string> argDictionary = new Dictionary<string, string>();
		
		var args = System.Environment.GetCommandLineArgs();
		
		for (int i = 0; i < args.Length; i++)
		{
			var arg = args[i].ToLower();
			
			if (arg.StartsWith("-"))
			{
				var value = i < args.Length - 1 ? args[i + 1].ToLower() : null;
				value = (value?.StartsWith("-") ?? false) ? null : value;
				
				argDictionary.Add(arg, value);
			}
		}
		
		return argDictionary;
	}
}
