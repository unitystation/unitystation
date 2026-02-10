using UnityEngine;
using US13.Core.Camera;

namespace US13.Player
{
	public class Ghost : MonoBehaviour
	{

		private PlayerScript PlayerScript;
		public void Awake()
		{
			PlayerScript = GetComponent<PlayerScript>();
			PlayerScript.OnBodyPossesedByPlayer.AddListener(PlayerEnterGhost);
		}

		public void PlayerEnterGhost()
		{
			Camera.main?.GetComponent<CameraEffectControlScript>()?.Stop();
		}
	}
}
