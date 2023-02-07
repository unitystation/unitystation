using System;
using UnityEngine;

namespace Items
{
	public class LastTouch : MonoBehaviour
	{
		public PlayerInfo LastTouchedBy { get; set; }

		private void OnDestroy()
		{
			LastTouchedBy = null;
		}

		private void SetLastTouch(GameObject player)
		{
			if (player.TryGetComponent<PlayerScript>(out var info) == false) return;
			LastTouchedBy = info.PlayerInfo;
		}
	}
}