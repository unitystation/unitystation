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
		[SerializeField] private List<FaithSO> PresetFaiths = new List<FaithSO>();
		private Faith currentSelectedFaith;

		private void Awake()
		{
			foreach (var faith in PresetFaiths)
			{
				var newFaith = Instantiate(FaithButtonTemplate, FaithList, faith);
				newFaith.GetComponent<FaithPresetButton>().Setup(faith.Faith, this);
			}
			SetFaith(PresetFaiths[0].Faith);
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
			foreach (GameObject gridButton in PropertiesGrid.transform)
			{
				Destroy(gridButton);
			}
			foreach (var property in faith.FaithProperties)
			{
				var newProp = Instantiate(PropertiesTemplate, PropertiesGrid, false);
				newProp.GetComponent<FaithPropertyHoverInfo>().Setup(property, this);
			}
		}
	}
}