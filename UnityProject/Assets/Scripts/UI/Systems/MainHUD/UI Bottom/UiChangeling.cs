using System;
using System.Collections.Generic;
using Logs;
using TMPro;
using UI.Core;
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

		public List<ChangelingBaseAbility> ChangelingDataToBuy => ChangelingMain.AllAbilities;

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

		public void AddAbility(ChangelingBaseAbility abilityToAdd)
		{
			ChangelingMain.CmdBuyAbility(abilityToAdd.Index);
		}

		public void OpenTransformUI(ChangelingMain changeling, Action<ChangelingDna> actionForUse)
		{
			var choise = new List<DynamicUIChoiceEntryData>();
			foreach (ChangelingDna x in changeling.ChangelingLastDNAs)
			{
				var newEntry = new DynamicUIChoiceEntryData();
				newEntry.Text = $"{x.PlayerName}";
				newEntry.ChoiceAction = () =>
				{
					actionForUse(x);
				};

				try
				{
					newEntry.Icon = OccupationList.Instance.Get(x.Job).PreviewSprite;
				} catch
				{
					Loggy.LogError("[UiChangeling/OpenTransformUI] Can`t pick preview sprite", Category.Changeling);
				}
				choise.Add(newEntry);
			}
			DynamicChoiceUI.ClientDisplayChoicesNotNetworked("Select DNA to transform", "Select DNA to transform", choise, true);
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