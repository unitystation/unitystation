using System;
using System.Runtime.InteropServices;
using UnityEngine;
using US13.Core.Database;
using US13.Managers;
using US13.Managers.SubSceneManager;
using Event = US13.Managers.Event;

namespace Util
{
	/// <summary>
	/// Changes the game's window title. Only works on windows sadly :(
	/// </summary>
	public class WindowNameChanger : MonoBehaviour
	{
		private void Awake()
		{
			if(EventManager.Instance == null) return;
			EventManager.AddHandler(Event.PlayerRejoined, ChangeTitle);
			EventManager.AddHandler(Event.RoundStarted, ChangeTitle);
		}

		private static void ChangeTitle()
		{
			var serverName = ServerData.ServerConfig.ServerName;
			var cool = $"{Application.productName} Build v{Application.version} - {serverName} ||" +
			                               $" {GameManager.Instance.GetGameModeName()} on {SubSceneManager.ServerChosenMainStation}";

			//TODO Think of some good way of supporting multiple systems without multiple DLLS for secure stuff
		}
	}
}