using AdminTools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.AdminTools
{
	public abstract class RespawnTab: MonoBehaviour
	{
		[SerializeField]protected PlayerManagePage playerManagePage;
		[SerializeField]protected Text playerName;
		[SerializeField]protected Dropdown dropdown;
		protected AdminPlayerEntry PlayerEntry;

		public Dropdown Dropdown => dropdown;

		public void SetPlayerEntry(AdminPlayerEntry playerEntry)
		{
			playerName.text = playerEntry.PlayerData.name;
			PlayerEntry = playerEntry;
		}

		public abstract void RequestRespawn();
	}
}