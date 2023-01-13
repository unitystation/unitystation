using UnityEngine;
using UI.Core.NetUI;
using Systems.Research.Data;
using UnityEngine.UI;
using Systems.Clearance;

namespace UI.Objects.Research
{
	public class GUI_FocusPage : NetPage
	{
		[SerializeField]
		private GUI_ResearchServer serverGUI;

		[SerializeField]
		private ClearanceRestricted clearanceRestricted;

		[SerializeField]
		private Dropdown focusDropDown;

		private TechType selectedFocus = TechType.None;

		[SerializeField]
		private NetText_label confirmLabel;

		[SerializeField]
		private NetText_label accessLabel;

		private const int FOCUS_COST = 10;

		private bool isFocusSet = false;

		public void UpdateGUI()
		{
			if (serverGUI.CurrentPage != this) return;

			focusDropDown.SetValueWithoutNotify((int)serverGUI.Server.UIselectedFocus - 1);
			accessLabel.MasterSetValue("");
			confirmLabel.MasterSetValue($"Confirm Focus ({FOCUS_COST}RP)");
		}

		public void ConfirmFocus(PlayerInfo Subject)
		{
			if (isFocusSet == true || serverGUI.TechWeb.researchPoints < FOCUS_COST || ValidateClearance(Subject.Mind.CurrentPlayScript.gameObject) == false) return;	

			isFocusSet = true;

			serverGUI.TechWeb.SubtractResearchPoints(FOCUS_COST);
			serverGUI.TechWeb.SetResearchFocus((TechType)serverGUI.Server.UIselectedFocus);
			serverGUI.OpenTechWebPage();
		}

		public void UpdateData()
		{
			selectedFocus = (TechType)(focusDropDown.value + 1);
			if (CustomNetworkManager.Instance._isServer == false) serverGUI.Server.CmdSetFocus(selectedFocus);
			else serverGUI.Server.SetFocusServer(selectedFocus);
		}

		public bool ValidateClearance(GameObject check)
		{
			if (clearanceRestricted.HasClearance(check) == true) return true;
			
			accessLabel.MasterSetValue("Research focus can only be assigned by the research director");
			return false;
			
		}
	}
}
