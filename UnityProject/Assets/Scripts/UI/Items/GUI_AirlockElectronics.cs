using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Items.Construction;

namespace UI.Items
{
	public class GUI_AirlockElectronics : NetTab
	{
		[Tooltip("List of accesses in the current category.")]
		public EmptyItemList CategoryContent;

		//ScriptableObject
		public AccessList GeneralCategory;
		public AccessList SecurityCategory;
		public AccessList MedbayCategory;
		public AccessList ResearchCategory;
		public AccessList EngineeringCategory;
		public AccessList SupplyCategory;
		public AccessList CommandCategory;

		public NetText_label CurrentAcceessText;

		private AirlockElectronics airlockElectronics;

		protected override void InitServer()
		{
			StartCoroutine(WaitForProvider());
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}
			airlockElectronics = Provider.GetComponentInChildren<AirlockElectronics>();
			UpdateCurrentAcceessText();
			OpenGeneralCategory();
		}

		private void OpenCategory(AccessList category)
		{
			List<Access> categoryAccesses = category.Accesses;
			int numberOfAccesses = categoryAccesses.Count;

			CategoryContent.Clear();
			CategoryContent.AddItems(numberOfAccesses);
			int i = 0;
			foreach (var access in categoryAccesses)
			{
				var accessButton = (GUI_AirlockElectronicsEntry)CategoryContent.Entries[i];
				accessButton.SetValues(access, this);
				i++;
			}
		}
		public void ServerSetAccess(Access accessToSet)
		{
			airlockElectronics.CurrentAccess = accessToSet;
			UpdateCurrentAcceessText();
		}
		private void UpdateCurrentAcceessText()
		{
			CurrentAcceessText.SetValueServer(airlockElectronics.CurrentAccess.ToString());
		}

		#region Buttons
		public void OpenGeneralCategory()
		{
			OpenCategory(GeneralCategory);
		}
		public void OpenSecurityCategory()
		{
			OpenCategory(SecurityCategory);
		}
		public void OpenMedbayCategory()
		{
			OpenCategory(MedbayCategory);
		}
		public void OpenResearchCategory()
		{
			OpenCategory(ResearchCategory);
		}
		public void OpenEngineeringCategory()
		{
			OpenCategory(EngineeringCategory);
		}
		public void OpenSupplyCategory()
		{
			OpenCategory(SupplyCategory);
		}
		public void OpenCommandCategory()
		{
			OpenCategory(CommandCategory);
		}
		#endregion
	}
}
