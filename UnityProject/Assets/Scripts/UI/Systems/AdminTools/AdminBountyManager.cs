using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AdminTools;
using Managers;
using Strings;
using Systems.Cargo;
using TMPro;


namespace UI.Systems.AdminTools
{
	public class AdminBountyManager : AdminPage
	{
		public static AdminBountyManager Instance;

		[SerializeField] private TMP_InputField bountyAmount;
		[SerializeField] private TMP_InputField bountyReward;
		[SerializeField] private TMP_InputField bountyDesc;
		[SerializeField] private Toggle bountyAnnoucementToggle;
		[SerializeField] private TMP_Dropdown itemTraitsForBounties;
		[SerializeField] private GameObject bountiesList;
		[SerializeField] private GameObject bountyEntryTemplate;
		[SerializeField] private GameObject bountiesManagerTab;
		[SerializeField] private GameObject bountiesAdderTab;

		private void Awake()
		{
			Instance = this;
		}

		private void Start()
		{
			var traitNames = new List<string>();
			foreach (var trait in CommonTraits.Instance.everyTraitOutThere)
			{
				traitNames.Add(trait.name);
			}
			itemTraitsForBounties.options.Clear();
			itemTraitsForBounties.AddOptions(traitNames);
		}

		private void OnDisable()
		{
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		public override void OnEnable()
		{
			base.OnEnable();
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
			ShowManager();
		}

		public void ShowManager()
		{
			bountiesAdderTab.SetActive(false);
			bountiesManagerTab.SetActive(true);
			CargoManager.Instance.CmdRequestServerData(PlayerManager.LocalPlayerScript.netIdentity.connectionToServer);
		}

		public void ShowAdder()
		{
			bountiesAdderTab.SetActive(true);
			bountiesManagerTab.SetActive(false);
		}

		public void HidePanel()
		{
			gameObject.SetActive(false);
		}

		public void RefreshBountiesList(List<CargoManager.BountySyncData> syncData)
		{
			foreach (Transform child in bountiesList.transform)
			{
				Destroy(child.gameObject);
			}

			foreach (var activeBounty in syncData)
			{
				var newEntry = Instantiate(bountyEntryTemplate, bountiesList.transform);
				newEntry.GetComponent<AdminBountyManagerListEntry>().Setup(activeBounty.Index, activeBounty.Desc, activeBounty.Reward);
				newEntry.SetActive(true);
			}
		}

		public void AddBounty()
		{
			foreach (var possibleTrait in CommonTraits.Instance.everyTraitOutThere)
			{
				if(possibleTrait.name != itemTraitsForBounties.options[itemTraitsForBounties.value].text) continue;
				CargoManager.Instance.CmdAddBounty(possibleTrait, int.Parse(bountyAmount.text),
					bountyDesc.text, int.Parse(bountyReward.text) , bountyAnnoucementToggle.isOn);
				bountyAnnoucementToggle.isOn = false;
				break;
			}
			//We ask to refresh the data we have first from the server.
			//(Max) : Should we have a short delay for slow connections? Or does the mirror await it for us?
			CargoManager.Instance.CmdRequestServerData(PlayerManager.LocalPlayerScript.netIdentity.connectionToServer);
		}
	}
}