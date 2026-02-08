using TMPro;
using UnityEngine;
using US13.UI.Systems.IngameMenu;

namespace US13.UI.Systems.ServerInfoPanel.Pages
{
	public class MotdPage: InfoPanelPage
	{
		[SerializeField] private TMP_Text serverName;
		[SerializeField] private TMP_Text serverDescription;
		[SerializeField] private GameObject discordButtonGo;
		private OpenURL discordButtonLink;
		private bool hasContent;

		private void Awake()
		{
			discordButtonLink = discordButtonGo.GetComponent<OpenURL>();
		}

		private void HideAllObjects()
		{
			serverName.gameObject.SetActive(false);
			serverDescription.gameObject.SetActive(false);
			discordButtonGo.SetActive(false);
		}

		public void PopulatePage(string sName, string description, string discordId)
		{
			HideAllObjects();
			if (string.IsNullOrEmpty(sName) == false)
			{
				serverName.gameObject.SetActive(true);
				serverName.text = sName;
			}

			if (string.IsNullOrEmpty(description) == false)
			{
				serverDescription.gameObject.SetActive(true);
				serverDescription.text = description;
			}

			if (string.IsNullOrEmpty(discordId))
			{
				return;
			}

			discordButtonGo.SetActive(true);
			discordButtonLink.url = $"https://discord.gg/{discordId}";
		}

		public override bool HasContent()
		{
			return string.IsNullOrEmpty(serverDescription.text) == false;
		}
	}
}