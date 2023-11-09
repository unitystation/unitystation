using System;
using Logs;
using TMPro;
using UnityEngine;

namespace Systems.Faith.UI
{
	public class FaithInfoUI : MonoBehaviour, IFaithPropertyUISetter
	{
		private Faith currentlyViewedFaith = null;

		[SerializeField] private TMP_Text faithName;
		[SerializeField] private TMP_Text pointsText;
		[SerializeField] private TMP_Text godName;
		[SerializeField] private TMP_Text mindnessText;
		[SerializeField] private TMP_Text faithDesc;
		[SerializeField] private TMP_Text membersText;
		[SerializeField] private Transform PropertiesGrid;
		[SerializeField] private Transform PropertiesTemplate;
		[SerializeField] private Transform PageMembers;
		[SerializeField] private Transform PageInfo;
		[SerializeField] private Transform PageHowTo;
		public string UnfocusedText { get; set; }

		public struct FaithUIInfo
		{
			public string FaithName;
			public string Points;
			public string Members;
		}

		public void Refresh()
		{
			PlayerManager.LocalPlayerScript.PlayerFaith.CmdUpdateInfoScreenData();
		}

		private void OnEnable()
		{
			Refresh();
		}

		public void UpdateData(FaithUIInfo info)
		{
			Faith faith = null;
			foreach (var presetFaith in FaithManager.Instance.AllFaiths)
			{
				if (info.FaithName != presetFaith.Faith.FaithName) continue;
				faith = presetFaith.Faith;
				break;
			}

			if (faith == null)
			{
				gameObject.SetActive(false);
				Loggy.LogError("[FaithInfoUI/UpdateData()] - No faith found with such name.");
				return;
			}

			currentlyViewedFaith = faith;
			faithName.text = info.FaithName;
			pointsText.text = $"Points: {info.Points}";
			godName.text = faith.GodName;
			mindnessText.text = faith.ToleranceToOtherFaiths.ToString();
			faithDesc.text = faith.FaithDesc;
			membersText.text = info.Members;
			UnfocusedText = faith.FaithDesc;
			UpdateProperties();
		}

		private void UpdateProperties()
		{
			for (int i = 0; i < PropertiesGrid.childCount; i++)
			{
				Destroy(PropertiesGrid.GetChild(i).gameObject);
			}
			foreach (var property in currentlyViewedFaith.FaithProperties)
			{
				var newProp = Instantiate(PropertiesTemplate, PropertiesGrid, false);
				newProp.GetComponent<FaithPropertyHoverInfo>().Setup(property, this);
			}
		}

		public void SetDesc(string desc)
		{
			faithDesc.text = desc;
		}

		public void OnChangePage(int page)
		{
			PageInfo.SetActive(false);
			PageMembers.SetActive(false);
			PageHowTo.SetActive(false);
			switch (page)
			{
				case 0:
					PageInfo.SetActive(true);
					break;
				case 1:
					PageMembers.SetActive(true);
					break;
				case 2:
					PageHowTo.SetActive(true);
					break;
				default:
					Loggy.LogError("No such page exists");
					break;
			}
		}
	}
}