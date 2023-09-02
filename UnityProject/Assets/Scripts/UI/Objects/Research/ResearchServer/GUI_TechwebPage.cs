using Logs;
using UnityEngine;
using UI.Core.NetUI;
using Systems.Research.Data;

namespace UI.Objects.Research
{
	public class GUI_TechwebPage : NetPage
	{
		[SerializeField]
		private GUI_ResearchServer serverGUI;

		[SerializeField]
		private EmptyItemList ResearchedTechList;

		[SerializeField]
		private EmptyItemList AvailableTechList;

		[SerializeField]
		private EmptyItemList FutureTechList;

		[SerializeField]
		private NetText_label PointLabel;

		[SerializeField]
		private NetText_label FocusLabel;

		public void UpdateGUI()
		{
			if (serverGUI.CurrentPage != this) return;

			UpdateResearchTechList();
			UpdateFutureTechList();
			UpdateAvailiableTechList();
			PointLabel.MasterSetValue($"Available Points: {serverGUI.TechWeb.researchPoints} (+{serverGUI.Server.ResearchPointsTrickle} / minute)");

			FocusLabel.MasterSetValue(serverGUI.TechWeb.ResearchFocus.ToString());
		}

		private void UpdateResearchTechList()
		{
			int researchedTechCount = serverGUI.TechWeb.ResearchedTech.Count;

			ResearchedTechList.SetItems(researchedTechCount);
			for (int i = 0; i < researchedTechCount; i++)
			{
				Technology technology = serverGUI.TechWeb.ResearchedTech[i];

				if (ResearchedTechList.Entries[i].TryGetComponent<ResearchedTechEntry>(out var entry)) entry.Initialise(AppendNameAndTechType(technology), technology.Description);
				else Loggy.LogError("GUI_ResearchServer.cs: Could not find ResearchTechEntry component on ResearchedTech Entry");
			}
		}

		private void UpdateFutureTechList()
		{
			int futureTechCount = serverGUI.TechWeb.FutureTech.Count;

			FutureTechList.SetItems(futureTechCount);
			for (int i = 0; i < futureTechCount; i++)
			{
				Technology technology = serverGUI.TechWeb.FutureTech[i];
				string description = technology.Description + "\n\nRequirements:";
				foreach (string prereq in technology.RequiredTechnologies)
				{
					description += $"\n-{prereq}";
				}

				if (FutureTechList.Entries[i].TryGetComponent<ResearchedTechEntry>(out var entry)) entry.Initialise(AppendNameAndTechType(technology), description);
				else Loggy.LogError("GUI_ResearchServer.cs: Could not find ResearchTechEntry component on FutureTech Entry");
			}
		}

		private void UpdateAvailiableTechList()
		{
			int availableTechCount = serverGUI.TechWeb.AvailableTech.Count;

			AvailableTechList.SetItems(availableTechCount);
			for (int i = 0; i < availableTechCount; i++)
			{
				Technology technology = serverGUI.TechWeb.AvailableTech[i];

				if (AvailableTechList.Entries[i].TryGetComponent<AvailableTechEntry>(out var entry)) entry.Initialise(technology, serverGUI.TechWeb);
				else Loggy.LogError("GUI_ResearchServer.cs: Could not find AvailableTechEntry component on AvailableTech Entry");
			}
		}

		public static string AppendNameAndTechType(Technology technology)
		{
			string prefix = string.Empty;
			switch(technology.techType)
			{
				case TechType.Robotics:
					prefix = "Rbt";
					break;

				case TechType.Machinery:
					prefix = "Mch";
					break;

				case TechType.Equipment:
					prefix = "Eqp";
					break;

				case TechType.Chemistry:
					prefix = "Chm";
					break;

				case TechType.None:
				default:
					prefix = "Non";
					break;
			}
			return $"[{prefix}] {technology.DisplayName}";
		}
	}
}
