using UnityEngine;
using UI.Core.NetUI;
using System.Collections;
using Systems.Research.Objects;
using Systems.Research.Data;
using Systems.Research;

namespace UI.Objects.Research
{
	public class GUI_ResearchServer : NetTab
	{
		[SerializeField]
		private EmptyItemList ResearchedTechList;

		[SerializeField]
		private EmptyItemList AvailableTechList;

		[SerializeField]
		private EmptyItemList FutureTechList;

		[SerializeField]
		private NetText_label PointLabel;

		private Techweb techWeb;
		private bool isUpdating = false;

		public void Awake()
		{
			StartCoroutine(WaitForProvider());
		}
		
		public void OnDestroy()
		{
			techWeb.UIupdate -= UpdateGUI;
		}

		private IEnumerator WaitForProvider()
		{
			while (Provider == null)
			{
				yield return WaitFor.EndOfFrame;
			}

			techWeb = Provider.GetComponent<ResearchServer>().Techweb;
			techWeb.UIupdate += UpdateGUI;

			if (!CustomNetworkManager.Instance._isServer) yield break;

			UpdateGUI();

			OnTabOpened.AddListener(UpdateGUIForPeepers);

		}

		public void UpdateGUIForPeepers(PlayerInfo notUsed)
		{
			if (!isUpdating)
			{
				isUpdating = true;
				StartCoroutine(WaitForClient());
			}
		}

		private IEnumerator WaitForClient()
		{
			yield return new WaitForSeconds(0.2f);
			UpdateGUI();
			isUpdating = false;
		}

		public void UpdateGUI()
		{
			UpdateResearchTechList();
			UpdateFutureTechList();
			UpdateAvailiableTechList();
			PointLabel.MasterSetValue($"Available Points: {techWeb.researchPoints} (+1 / minute)");
		}

		private void UpdateResearchTechList()
		{
			int researchedTechCount = techWeb.ResearchedTech.Count;

			ResearchedTechList.SetItems(researchedTechCount);
			for(int i = 0; i < researchedTechCount; i++)
			{
				Technology technology = techWeb.ResearchedTech[i];
				ResearchedTechList.Entries[i].GetComponent<ResearchedTechEntry>().Initialise(technology.DisplayName,technology.Description);
			}
		}

		private void UpdateFutureTechList()
		{
			int futureTechCount = techWeb.FutureTech.Count;

			FutureTechList.SetItems(futureTechCount);
			for (int i = 0; i < futureTechCount; i++)
			{
				Technology technology = techWeb.FutureTech[i];
				string description = technology.Description + "\n\nRequirements:";
				foreach(string prereq in technology.RequiredTechnologies)
				{
					description += $"\n-{prereq}";
				}

				FutureTechList.Entries[i].GetComponent<ResearchedTechEntry>().Initialise(technology.DisplayName, description);
			}
		}

		private void UpdateAvailiableTechList()
		{
			int availableTechCount = techWeb.AvailableTech.Count;

			AvailableTechList.SetItems(availableTechCount);
			for (int i = 0; i < availableTechCount; i++)
			{
				Technology technology = techWeb.AvailableTech[i];
				AvailableTechList.Entries[i].GetComponent<AvailableTechEntry>().Initialise(technology, techWeb);
			}
		}

	}
}
