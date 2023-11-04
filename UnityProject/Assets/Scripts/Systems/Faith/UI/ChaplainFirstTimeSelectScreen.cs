using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Systems.Faith.UI
{
	public class ChaplainFirstTimeSelectScreen : MonoBehaviour
	{
		public string UnfocusedDescText { get; private set; }
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
			UnfocusedDescText = faith.FaithDesc;
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
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdJoinFaith(currentSelectedFaith.FaithName);
			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdSetMainFaith();
		}
	}
}