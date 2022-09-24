using System;
using System.Runtime.InteropServices;
using DatabaseAPI;
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
		private static extern bool SetWindowText(IntPtr hwnd, string lpString);
		[DllImport("user32.dll", EntryPoint = "FindWindow")]
		private static extern IntPtr FindWindow(string className, string windowName);


		private void Awake()
		{
			if(EventManager.Instance == null) return;
			EventManager.AddHandler(Event.PlayerRejoined, ChangeTitle);
			EventManager.AddHandler(Event.RoundStarted, ChangeTitle);
		}

		private static void ChangeTitle()
		{
			//Get the window handle.
			var windowPtr = FindWindow(null, Application.productName);
			//Set the title text using the window handle.
			var serverName = ServerData.ServerConfig.ServerName;
			SetWindowText(windowPtr, $"{Application.productName} Build v{Application.version} - {serverName} ||" +
			                         $" {GameManager.Instance.GetGameModeName()} on {SubSceneManager.ServerChosenMainStation}");
		}

#endif

	}
}