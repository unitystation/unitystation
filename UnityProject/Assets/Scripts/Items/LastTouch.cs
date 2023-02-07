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
	}
}