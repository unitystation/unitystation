using System;
using System.Collections.Generic;
using AdminCommands;
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
		[SerializeField] private TMP_InputField bountyTitle;
		public TMP_InputField budgetInput;
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

		private void Update()
		{
			if(Input.GetKeyDown(KeyCode.Return) == false) return;
			var newBudget = int.Parse(budgetInput.text);
			if(newBudget < 0) return;
			AdminCommandsManager.Instance.CmdChangeBudget(newBudget);
		}

		private void OnDisable()
		{
			UIManager.IsInputFocus = false;
			UIManager.PreventChatInput = false;
		}

		public override void OnEnable()
		{
			base.OnEnable();
			AdminCommandsManager.Instance.CmdRequestCargoServerData();
			UIManager.IsInputFocus = true;
			UIManager.PreventChatInput = true;
		}

		public void ShowManager()
		{
			bountiesAdderTab.SetActive(false);
			bountiesManagerTab.SetActive(true);
			AdminCommandsManager.Instance.CmdRequestCargoServerData();
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

		public void RefreshBountiesList(List<CargoManager.BountySyncData> data)
		{
			ClearBountiesList();

			foreach (var activeBounty in data)
			{
				var newEntry = Instantiate(bountyEntryTemplate, bountiesList.transform);
				newEntry.GetComponent<AdminBountyManagerListEntry>().Setup(activeBounty.Index, activeBounty.Title, activeBounty.Reward, this);
				newEntry.SetActive(true);
			}
		}

		public void ClearBountiesList()
		{
			foreach (Transform child in bountiesList.transform)
			{
				Destroy(child.gameObject);
			}
		}

		public void AddBounty()
		{
			foreach (var possibleTrait in CommonTraits.Instance.everyTraitOutThere)
			{
				if(possibleTrait.name != itemTraitsForBounties.options[itemTraitsForBounties.value].text) continue;
				AdminCommandsManager.Instance.CmdAddBounty(possibleTrait, int.Parse(bountyAmount.text), bountyTitle.text,
					bountyDesc.text, int.Parse(bountyReward.text) , bountyAnnoucementToggle.isOn);
				bountyAnnoucementToggle.isOn = false;
				break;
			}

			AdminCommandsManager.Instance.CmdRequestCargoServerData();
		}
	}
}