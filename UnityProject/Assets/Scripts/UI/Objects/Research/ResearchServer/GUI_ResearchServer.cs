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

		private Techweb techWeb => server.Techweb;
		private ResearchServer server;

		private bool isUpdating = false;

		private const float CLIENT_UPDATE_DELAY = 0.2f;

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

			server = Provider.GetComponent<ResearchServer>();
			techWeb.UIupdate += UpdateGUI;

			if (CustomNetworkManager.Instance._isServer == false) yield break;

			UpdateGUI();

			OnTabOpened.AddListener(UpdateGUIForPeepers);

		}

		public void UpdateGUIForPeepers(PlayerInfo notUsed)
		{
			if (isUpdating == false)
			{
				isUpdating = true;
				StartCoroutine(WaitForClient());
			}
		}

		private IEnumerator WaitForClient()
		{
			yield return new WaitForSeconds(CLIENT_UPDATE_DELAY);
			UpdateGUI();
			isUpdating = false;
		}

		public void UpdateGUI()
		{
			UpdateResearchTechList();
			UpdateFutureTechList();
			UpdateAvailiableTechList();
			PointLabel.MasterSetValue($"Available Points: {techWeb.researchPoints} (+{server.ResearchPointsTrickle} / minute)");
		}

		private void UpdateResearchTechList()
		{
			int researchedTechCount = techWeb.ResearchedTech.Count;

			ResearchedTechList.SetItems(researchedTechCount);
			for(int i = 0; i < researchedTechCount; i++)
			{
				Technology technology = techWeb.ResearchedTech[i];

				if(ResearchedTechList.Entries[i].TryGetComponent<ResearchedTechEntry>(out var entry)) entry.Initialise(technology.DisplayName,technology.Description);
				else Logger.LogError("GUI_ResearchServer.cs: Could not find ResearchTechEntry component on ResearchedTech Entry");
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

				if (FutureTechList.Entries[i].TryGetComponent<ResearchedTechEntry>(out var entry)) entry.Initialise(technology.DisplayName, description);
				else Logger.LogError("GUI_ResearchServer.cs: Could not find ResearchTechEntry component on FutureTech Entry");
			}
		}

		private void UpdateAvailiableTechList()
		{
			int availableTechCount = techWeb.AvailableTech.Count;

			AvailableTechList.SetItems(availableTechCount);
			for (int i = 0; i < availableTechCount; i++)
			{
				Technology technology = techWeb.AvailableTech[i];

				if(AvailableTechList.Entries[i].TryGetComponent<AvailableTechEntry>(out var entry)) entry.Initialise(technology, techWeb);
				else Logger.LogError("GUI_ResearchServer.cs: Could not find AvailableTechEntry component on AvailableTech Entry");
			}
		}

	}
}
