using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "PlayerStatesSingleton", menuName = "Singleton/PlayerStatesSingleton", order = 0)]
	public class PlayerTypeSingleton : SingletonScriptableObject<PlayerTypeSingleton>
	{
		[Tooltip("List of playerSettings")]
		[SerializeField]
		private List<PlayerTypeSettings> playerSettings = new List<PlayerTypeSettings>();

		private Dictionary<PlayerTypes, PlayerTypeSettings> playerSettingsDict = new Dictionary<PlayerTypes, PlayerTypeSettings>();

		private void Awake()
		{
			playerSettingsDict.Clear();

			foreach (var playerSetting in playerSettings)
			{
				playerSettingsDict.Add(playerSetting.PlayerType, playerSetting);
			}
		}

		public PlayerTypes DoCheck(Predicate<PlayerTypeSettings> toCheck)
		{
			var state = PlayerTypes.None;

			foreach (var playerSetting in playerSettings)
			{
				if (toCheck.Invoke(playerSetting))
				{
					state |= playerSetting.PlayerType;
				}
			}

			return state;
		}
	}
}