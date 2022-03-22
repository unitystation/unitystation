using System;
using System.Runtime.InteropServices;
using DatabaseAPI;
using ServerInfo;
using UnityEngine;

namespace Util
{
	/// <summary>
	/// Changes the game's window title. Only works on windows sadly :(
	/// </summary>
	public class WindowNameChanger : MonoBehaviour
	{
#if UNITY_STANDALONE_WIN


		//Import the following.
		[DllImport("user32.dll", EntryPoint = "SetWindowText")]
		public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
		[DllImport("user32.dll", EntryPoint = "FindWindow")]
		public static extern System.IntPtr FindWindow(System.String className, System.String windowName);


		private void Awake()
		{
			if(EventManager.Instance == null) return;
			EventManager.AddHandler(Event.PlayerRejoined, ChangeTile);
			EventManager.AddHandler(Event.RoundStarted, ChangeTile);
		}

		private void ChangeTile()
		{
			//Get the window handle.
			var windowPtr = FindWindow(null, Application.productName);
			//Set the title text using the window handle.
			var serverName = ServerInfoUI.Instance.ServerName.text != "" ? ServerInfoUI.Instance.ServerName.text : "Nameless Server";
			SetWindowText(windowPtr, $"{Application.productName} Build v{Application.version} - {serverName} ||" +
			                         $" {GameManager.Instance.GetGameModeName()} on {SubSceneManager.ServerChosenMainStation}");
		}

#endif

	}
}