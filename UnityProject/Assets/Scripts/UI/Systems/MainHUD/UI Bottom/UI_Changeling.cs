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
	public class UI_Changeling : MonoBehaviour
	{
		//private ChangelingMain ChangelingMain;
		public ChangelingMain ChangelingMain;
		public static UI_Changeling instance;
		[SerializeField] private TMP_Text chemText = null;
		[SerializeField] private GameObject chems = null;
		[SerializeField] private GameObject storeGameObject = null;
		[SerializeField] private UI_ChangelingStore store = null;

		[SerializeField] private GameObject memoriesGameObject = null;
		[SerializeField] private UI_ChangelingMemories memories = null;

		[SerializeField] private TMP_Text abilityPointsCount = null;
		//private List<ActionData> actData = new List<ActionData>();

		public List<ChangelingData> ChangelingDataToBuy => ChangelingMain.AllAbilities;

		private void Awake()
		{
			instance = this;
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
			RefreshUI();
			chems.SetActive(true);
		}

		public void ResetAbilites()
		{
			ChangelingMain.ResetAbilities();
			store.Refresh(ChangelingDataToBuy, ChangelingMain);
		}

		public void UpdateChemText()
		{
			chemText.text = ChangelingMain.Chem.ToString();
		}

		public void UpdateEPText()
		{
			abilityPointsCount.text = $"Left genetic points {ChangelingMain.EvolutionPoints}"; //changelingMain.EpPoints.ToString();
		}

		public void TurnOff()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RefreshUI);

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

		public void RefreshUI()
		{
			try
			{
				UpdateChemText();
				UpdateEPText();
			}
			catch (Exception)
			{
				TurnOff();
			}
		}

		public void OpenTransformUI(ChangelingMain changeling, Action<ChangelingDNA> actionForUse)
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
					ChangelingDNA x = changeling.ChangelingLastDNAs[i];
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