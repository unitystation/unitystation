using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Systems.Antagonists;
using TMPro;
using UI.Core.Action;
using UnityEngine;

namespace Changeling
{
	public class UI_Changeling : MonoBehaviour
	{
		ChangelingMain changelingMain;
		[SerializeField] private TMP_Text chemText = null;
		[SerializeField] private GameObject chems = null;
		[SerializeField] private GameObject store = null;
		//private List<ActionData> actData = new List<ActionData>();

		//public List<ActionData> ActionData => actData;

		private void Awake()
		{
			if (changelingMain == null)
				TurnOff();
		}

		public void SetUp(ChangelingMain changeling)
		{
			changelingMain = changeling;
			RefreshUI();
			chems.SetActive(true);

			UpdateManager.Add(RefreshUI, 1f);
		}

		public void DisplayAbilityStore()
		{

		}

		public void UpdateChemText()
		{
			chemText.text = changelingMain.Chem.ToString();
		}

		public void TurnOff()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, RefreshUI);

			if (chems != null)
				chems.SetActive(false);
			if (store != null)
				store.SetActive(false);
			gameObject.SetActive(false);
		}

		public void AddAction(ChangelingData abilityToAdd)
		{
			if (changelingMain.GetAbilitesBuyed().Contains(abilityToAdd))
			{
				// then... what? TODO bruh
			}
		}

		public void RefreshUI()
		{
			UpdateChemText();
		}

		public void OpenTransformUI(ChangelingMain changeling, ChangelingAbilityTransform transformAbility)
		{

		}
	}
}