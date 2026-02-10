using UnityEngine;
using UnityEngine.UI;

namespace US13.UI.Systems.AdminTools
{
	public class AdminChatEntry : MonoBehaviour
	{
		[SerializeField] private Text msgText = null;

		public void SetText(string msg)
		{
			msgText.text = msg;
		}
	}
}
