using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Systems.Antagonists;
using TMPro;
using UI.Core;
using UI.Core.Action;
using UnityEngine;

namespace Changeling
{
	public class UiChangeling : MonoBehaviour
	{
		public ChangelingMain ChangelingMain;
		[SerializeField] private TMP_Text chemText = null;
		[SerializeField] private GameObject chems = null;
		[SerializeField] private GameObject storeGameObject = null;
		[SerializeField] private UiChangelingStore store = null;

		[SerializeField] private GameObject memoriesGameObject = null;
		[SerializeField] private UiChangelingMemories memories = null;

		[SerializeField] private TMP_Text abilityPointsCount = null;

		public List<ChangelingData> ChangelingDataToBuy => ChangelingMain.AllAbilities;

		private void Awake()
		{
			if (ChangelingMain == null)
				TurnOff();
		}

		public void OnDisable()
		{
			TurnOff();
		}

		public void SetUp(ChangelingMain changeling)
		{
			ChangelingMain = changeling;
			UpdateChemText();
			UpdateEPText();
			chems.SetActive(true);
		}

		public void ResetAbilites()
		{
			ChangelingMain.ResetAbilities();
		}

		public void RefreshAbilites()
		{
			store.Refresh(ChangelingDataToBuy, ChangelingMain);
		}

		public void UpdateChemText()
		{
			chemText.text = ChangelingMain.Chem.ToString();
		}

		public void UpdateEPText()
		{
			abilityPointsCount.text = $"Left genetic points {ChangelingMain.EvolutionPoints}";
		}

		public void TurnOff()
		{
			if (chems != null)
				chems.SetActive(false);
			if (storeGameObject != null)
				storeGameObject.SetActive(false);
			if (memoriesGameObject != null)
				memoriesGameObject.SetActive(false);
			gameObject.SetActive(false);
		}

		public void AddAbility(ChangelingData abilityToAdd)
		{
			ChangelingMain.CmdBuyAbility(abilityToAdd.Index);
		}

		public void OpenTransformUI(ChangelingMain changeling, Action<ChangelingDna> actionForUse)
		{
			var choise = new List<DynamicUIChoiceEntryData>();
			for (int i = 0; i < changeling.ChangelingLastDNAs.Count + 1; i++)
			{
				var newEntry = new DynamicUIChoiceEntryData();
				if (i == changeling.ChangelingLastDNAs.Count)
				{
					newEntry.Text = $"Back";
				} else
				{
					ChangelingDna x = changeling.ChangelingLastDNAs[i];
					newEntry.Text = $"{x.PlayerName}";
					newEntry.ChoiceAction = () =>
					{
						actionForUse(x);
					};

					newEntry.Icon = OccupationList.Instance.Get(x.Job).PreviewSprite;
				}

				choise.Add(newEntry);
			}
			DynamicChoiceUI.ClientDisplayChoicesNotNetworked("Choise transform DNA", "Choise transform DNA", choise);
		}

		public void OpenMemoriesUI()
		{
			if (ChangelingMain.ChangelingMemories.Count == 0)
				return;
			memoriesGameObject.SetActive(true);
			memories.Refresh(ChangelingMain.ChangelingMemories, ChangelingMain);
			CloseStoreUI();
		}

		public void OpenStoreUI()
		{
			if (ChangelingMain.AbilitiesNowData.Count == 0)
				return;
			storeGameObject.SetActive(true);
			store.Refresh(ChangelingDataToBuy, ChangelingMain);
			CloseMemoriesUI();
		}

		public void CloseStoreUI()
		{
			storeGameObject.SetActive(false);
		}

		public void CloseMemoriesUI()
		{
			memoriesGameObject.SetActive(false);
		}

		public void UpdateResetButton()
		{
			store.UpdateResetButton();
		}
	}
}