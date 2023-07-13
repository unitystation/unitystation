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
		ChangelingMain changelingMain;
		public static UI_Changeling instance;
		[SerializeField] private TMP_Text chemText = null;
		[SerializeField] private GameObject chems = null;
		[SerializeField] private GameObject storeGameObject = null;
		[SerializeField] private UI_ChangelingStore store = null;

		[SerializeField] private TMP_Text abilityPointsCount = null;
		//private List<ActionData> actData = new List<ActionData>();

		public List<ChangelingData> ChangelingDataToBuy => changelingMain.AllAbilities;

		private void Awake()
		{
			instance = this;
			if (changelingMain == null)
				TurnOff();
		}

		public void OnDisable()
		{
			TurnOff();
		}

		public void SetUp(ChangelingMain changeling)
		{
			changelingMain = changeling;
			RefreshUI();
			chems.SetActive(true);

			UpdateManager.Add(RefreshUI, 1f);
		}

		public void ResetAbilites()
		{
			changelingMain.ResetAbilites();
			store.Refresh(ChangelingDataToBuy, changelingMain);
		}

		public void UpdateChemText()
		{
			chemText.text = changelingMain.Chem.ToString();
		}

		public void UpdateEPText()
		{
			abilityPointsCount.text = $"Left genetic points {changelingMain.EvolutionPoints}"; //changelingMain.EpPoints.ToString();
		}

		public void TurnOff()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RefreshUI);

			if (chems != null)
				chems.SetActive(false);
			if (storeGameObject != null)
				storeGameObject.SetActive(false);
			gameObject.SetActive(false);
		}

		public void AddAbility(ChangelingData abilityToAdd)
		{
			changelingMain.AddAbility(abilityToAdd);
			RefreshUI();
		}

		public void RefreshUI()
		{
			try
			{
				UpdateChemText();
				UpdateEPText();
			} catch (Exception)
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
					newEntry.Text = $"{x.PlayerData.visibleName}";
					newEntry.ChoiceAction = () =>
					{
						actionForUse(x);
					};
				}

				choise.Add(newEntry);
			}
			DynamicChoiceUI.ClientDisplayChoicesNotNetworked("Choise transform DNA", "Choise transform DNA", choise);
		}

		public void OpenStoreUI()
		{
			storeGameObject.SetActive(true);
			store.Refresh(ChangelingDataToBuy, changelingMain);
		}

		[ContextMenu("CloseStoreUI")]
		public void CloseStoreUI()
		{
			storeGameObject.SetActive(false);
		}
	}
}