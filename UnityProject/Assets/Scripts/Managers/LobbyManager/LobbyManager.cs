using UnityEngine;

namespace Lobby
{
	public class LobbyManager : MonoBehaviour
	{
		public static LobbyManager Instance;
		public AccountLogin accountLogin;

		void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}
	}
}