using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "PlayerStatesSingleton", menuName = "Singleton/PlayerStatesSingleton", order = 0)]
	public class PlayerStatesSingleton : SingletonScriptableObject<PlayerStatesSingleton>
	{
		[Tooltip("List of playerSettings")]
		[SerializeField]
		private List<PlayerStateSettings> playerSettings = new List<PlayerStateSettings>();
		public List<PlayerStateSettings> PlayerSettings => playerSettings;

		private Dictionary<PlayerStates, PlayerStateSettings> playerSettingsDict = new Dictionary<PlayerStates, PlayerStateSettings>();

		public PlayerStatesSingleton I => Instance;

		private void Awake()
		{
			playerSettingsDict.Clear();

			foreach (var playerSetting in playerSettings)
			{
				playerSettingsDict.Add(playerSetting.PlayerState, playerSetting);
			}
		}

		public PlayerStates DoCheck(Predicate<PlayerStateSettings> toCheck)
		{
			var state = PlayerStates.None;

			foreach (var playerSetting in playerSettings)
			{
				if (toCheck.Invoke(playerSetting))
				{
					state |= playerSetting.PlayerState;
				}
			}

			return state;
		}
	}
}