using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UI.Core.NetUI;
using Items.Construction;
using Systems.Clearance;

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

		public NetLabel CurrentAcceessText;

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
			List<Clearance> categoryClearances = category.Clearances;
			int numberOfAccesses = categoryClearances.Count;

			CategoryContent.Clear();
			CategoryContent.AddItems(numberOfAccesses);
			int i = 0;
			foreach (var clearance in categoryClearances)
			{
				var accessButton = (GUI_AirlockElectronicsEntry)CategoryContent.Entries[i];
				accessButton.SetValues(clearance, this);
				i++;
			}
		}
		public void ServerSetAccess(Clearance clearanceToSet)
		{
			airlockElectronics.CurrentClearance = clearanceToSet;
			UpdateCurrentAcceessText();
		}
		private void UpdateCurrentAcceessText()
		{
			CurrentAcceessText.SetValueServer(airlockElectronics.CurrentClearance.ToString());
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
