using UnityEngine;
using US13.Managers;

namespace US13.Items
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