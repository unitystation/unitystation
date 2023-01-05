using UI.Core.NetUI;
using UnityEngine;
using Systems.Research.Data;
using Systems.Research;

namespace UI.Objects.Research
{
	public class AvailableTechEntry : DynamicEntry
	{
		[SerializeField] private NetText_label techDescription;
		[SerializeField] private NetText_label techName;
		[SerializeField] private NetText_label techPrice;
		[SerializeField] private EmptyItemList spriteList;

		private Technology technologyToUnlock;
		private Techweb techWeb;
		private CustomNetworkManager networkManager;

		public void Initialise(Technology technology, Techweb techWeb)
		{
			networkManager = CustomNetworkManager.Instance;
			technologyToUnlock = technology;
			this.techWeb = techWeb;

			techName.MasterSetValue(technology.DisplayName);
			techDescription.MasterSetValue(technology.Description);
			techPrice.MasterSetValue(technology.ResearchCosts.ToString());
			SetSprites();
		}

		//Will show the sprites of all things this technology will unlock, current commented out because networking is a mother fucker
		private void SetSprites()
		{
			/*int unlockCount = technologyToUnlock.DesignIDs.Count;
			spriteList.SetItems(unlockCount);
			for(int i = 0; i < unlockCount; i++)
			{
				string DesignID = technologyToUnlock.DesignIDs[i];
				if (Designs.Globals.InternalIDSearch.ContainsKey(DesignID)) continue;

				Design designClass = Designs.Globals.InternalIDSearch[DesignID];
				Sprite sprite = networkManager.ForeverIDLookupSpawnablePrefabs[designClass.ItemID].GetComponentInChildren<SpriteHandler>().CurrentSprite;

				spriteList.Entries[unlockCount].sprite = sprite //this line does not work, but its what is want to be dont
			}*/


		}

		public void TryResearchTech()
		{
			techWeb.ResearchTechology(technologyToUnlock);
		}
	}
}
