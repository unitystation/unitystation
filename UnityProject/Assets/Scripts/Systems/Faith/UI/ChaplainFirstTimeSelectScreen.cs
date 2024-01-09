﻿using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.Faith.UI
{
	public class ChaplainFirstTimeSelectScreen : MonoBehaviour, IFaithPropertyUISetter
	{
		[SerializeField] private TMP_Text FaithName;
		[SerializeField] private TMP_Text FaithDesc;
		[SerializeField] private Image FaithIcon;
		[SerializeField] private Transform PropertiesGrid;
		[SerializeField] private Transform PropertiesTemplate;
		[SerializeField] private Transform FaithList;
		[SerializeField] private Transform FaithButtonTemplate;
		private Faith currentSelectedFaith;

		private void Awake()
		{
			foreach (var faith in FaithManager.Instance.AllFaiths)
			{
				var newFaith = Instantiate(FaithButtonTemplate, FaithList, faith);
				newFaith.GetComponent<FaithPresetButton>().Setup(faith.Faith, this);
			}
			SetFaith(FaithManager.Instance.AllFaiths[0].Faith);
		}

		public string UnfocusedText { get; set; }

		public void SetDesc(string text)
		{
			FaithDesc.text = text;
		}

		public void SetFaith(Faith faith)
		{
			currentSelectedFaith = faith;
			FaithName.text = faith.FaithName;
			FaithDesc.text = faith.FaithDesc;
			FaithIcon.sprite = faith.FaithIcon;
			UnfocusedText = faith.FaithDesc;
			for (int i = 0; i < PropertiesGrid.childCount; i++)
			{
				Destroy(PropertiesGrid.GetChild(i).gameObject);
			}
			foreach (var property in faith.FaithProperties)
			{
				var newProp = Instantiate(PropertiesTemplate, PropertiesGrid, false);
				newProp.GetComponent<FaithPropertyHoverInfo>().Setup(property, this);
			}
		}

		public void OnChooseFaith()
		{
			gameObject.SetActive(false);
			PlayerManager.LocalPlayerScript.PlayerFaith.CreateNewFaith(currentSelectedFaith.FaithName);
			PlayerManager.LocalPlayerScript.PlayerFaith.JoinReligion(currentSelectedFaith.FaithName);
			PlayerManager.LocalPlayerScript.PlayerFaith.AddNewFaithLeader();
		}
	}
}