using DatabaseAPI;
using UnityEngine;

namespace Lobby
{
	public class LobbyManager : MonoBehaviour
	{
		public static LobbyManager Instance;
		public AccountLogin accountLogin;
		public CharacterCustomization characterCustomization;

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

		void OnEnable()
		{
			EventManager.AddHandler(EVENT.LoggedOut, SetOnLogOut);
		}

		void OnDisable()
		{
			EventManager.RemoveHandler(EVENT.LoggedOut, SetOnLogOut);
		}

		private void SetOnLogOut()
		{
			characterCustomization.gameObject.SetActive(false);
			accountLogin.gameObject.SetActive(true);
		}

	}
}