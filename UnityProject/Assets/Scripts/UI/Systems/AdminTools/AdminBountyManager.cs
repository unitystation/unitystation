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
		[SerializeField] private TMP_InputField bountyAmount;
		[SerializeField] private TMP_InputField bountyReward;
		[SerializeField] private TMP_InputField bountyDesc;
		[SerializeField] private Toggle bountyAnnoucementToggle;
		[SerializeField] private TMP_Dropdown itemTraitsForBounties;
		[SerializeField] private GameObject bountiesList;
		[SerializeField] private GameObject bountyEntryTemplate;
		[SerializeField] private GameObject bountiesManagerTab;
		[SerializeField] private GameObject bountiesAdderTab;


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
			RefreshBountiesList();
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
		}

		public void ShowManager()
		{
			bountiesAdderTab.SetActive(false);
			bountiesManagerTab.SetActive(true);
			RefreshBountiesList();
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

		private void RefreshBountiesList()
		{
			foreach (Transform child in bountiesList.transform)
			{
				Destroy(child.gameObject);
			}

			foreach (var activeBounty in CargoManager.Instance.ActiveBounties)
			{
				var newEntry = Instantiate(bountyEntryTemplate, bountiesList.transform);
				newEntry.GetComponent<AdminBountyManagerListEntry>().Setup(activeBounty);
				newEntry.SetActive(true);
			}
		}

		public void AddBounty()
		{
			foreach (var possibleTrait in CommonTraits.Instance.everyTraitOutThere)
			{
				if(possibleTrait.name != itemTraitsForBounties.options[itemTraitsForBounties.value].text) continue;
				if (CargoManager.Instance.AddBounty(possibleTrait, int.Parse(bountyAmount.text),
					    bountyDesc.text, int.Parse(bountyReward.text)) && bountyAnnoucementToggle.isOn)
				{
					CentComm.MakeAnnouncement(ChatTemplates.CentcomAnnounce, "A bounty for cargo has been issued from central communications", CentComm.UpdateSound.Notice);
					bountyAnnoucementToggle.isOn = false;
				}

				break;
			}
			RefreshBountiesList();
		}
	}
}