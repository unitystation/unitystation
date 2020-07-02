using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;

namespace GameConfig
{
	/// <summary>
	/// Config for in game stuff
	/// </summary>
	public class GameConfigManager : MonoBehaviour
	{
		private static GameConfigManager instance;
		public static GameConfigManager Instance => instance;

		private GameConfig config;

		public UnityEvent gameConfigLoaded = new UnityEvent();

		public static GameConfig GameConfig
		{
			get
			{
				return Instance.config;
			}
		}

		private void Awake()
		{
			if (instance == null)
			{
				instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		private void Start()
		{
			//Load as well in start so other scripts can subscribe to event in their awake and let it still be called.
			AttemptConfigLoad();
		}

		private void AttemptConfigLoad()
		{
			var path = Path.Combine(Application.streamingAssetsPath, "config", "gameConfig.json");

			if (File.Exists(path))
			{
				Debug.Log("1");
				config = JsonUtility.FromJson<GameConfig>(File.ReadAllText(path));

				//config loaded event
				gameConfigLoaded.Invoke();
			}
		}
	}

	[Serializable]
	public class GameConfig
	{
		public bool RandomEventsAllowed;
	}
}