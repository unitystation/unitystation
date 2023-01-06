using UI.Core.Net.Elements;
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

		private void SetSprites()
		{
			int unlockCount = technologyToUnlock.DesignIDs.Count;
			spriteList.SetItems(unlockCount);
		
			for(int i = 0; i < unlockCount; i++)
			{
				string DesignID = technologyToUnlock.DesignIDs[i]; //Gets the designs this research will unlock
				if (Designs.Globals.InternalIDSearch.ContainsKey(DesignID) == false) continue;

				Design designClass = Designs.Globals.InternalIDSearch[DesignID];
		
				//Gets the sprite of the gameObject that design is for
				SpriteDataSO sprite = networkManager.ForeverIDLookupSpawnablePrefabs[designClass.ItemID].GetComponentInChildren<SpriteHandler>().PresentSpritesSet;
		
				//Uses the sprite from above and sets the sprite of the list entry to that sprite
				spriteList.Entries[i].GetComponentInChildren<NetSpriteHandler>().SetValue(SpriteCatalogue.Instance.Catalogue.IndexOf(sprite));
			}
		}

		public void TryResearchTech()
		{
			techWeb.ResearchTechology(technologyToUnlock);
		}
	}
}
