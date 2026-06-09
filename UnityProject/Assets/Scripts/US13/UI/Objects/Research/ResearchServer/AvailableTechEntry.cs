using System.Collections;
using UnityEngine;
using US13.Core.Sprite_Handler;
using US13.Managers.NetworkManagement;
using US13.Systems.Research;
using US13.Systems.Research.Data;
using US13.UI.Core;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;
using Util;

namespace US13.UI.Objects.Research.ResearchServer
{
	public class AvailableTechEntry : DynamicEntry
	{
		[SerializeField] private NetText_label techDescription;
		[SerializeField] private NetText_label techName;
		[SerializeField] private NetText_label techPrice;
		[SerializeField] private EmptyItemList spriteList;


		private Technology technologyToUnlock;
		private US13.Systems.Research.Objects.ResearchServer server;
		private CustomNetworkManager networkManager;


		public void Initialise(Technology technology, US13.Systems.Research.Objects.ResearchServer server)
		{
			networkManager = CustomNetworkManager.Instance;
			technologyToUnlock = technology;
			this.server = server;

			techName.MasterSetValue(GUI_TechwebPage.AppendNameAndTechType(technology));
			techDescription.MasterSetValue(technology.Description);
			techPrice.MasterSetValue(technology.ResearchCosts.ToString());
			StartCoroutine(SetSprites());
		}

		private IEnumerator SetSprites()
		{
			int unlockCount = technologyToUnlock.DesignIDs.Count;

			yield return new WaitForEndOfFrame();
			spriteList.SetItems(unlockCount);
			yield return new WaitForEndOfFrame();

			for (int i = 0; i < unlockCount; i++)
			{
				if (spriteList.Entries[i].TryGetComponent<SpriteEntry>(out var handler) == false) continue;

				string DesignID = technologyToUnlock.DesignIDs[i]; //Gets the designs this research will unlock
				if (Designs.Globals.InternalIDSearch.ContainsKey(DesignID) == false) continue;

				Design designClass = Designs.Globals.InternalIDSearch[DesignID];

				//Gets the sprite of the gameObject that design is for
				GameObject designObject = networkManager.ForeverIDLookupSpawnablePrefabs[designClass.ItemID];
				SpriteDataSO sprite = designObject.GetComponentInChildren<SpriteHandler>().initialPresentSpriteSet;

				//Uses the sprite from above and sets the sprite of the list entry to that sprite
				handler.Initialise(sprite, designObject.ExpensiveName());

				yield return new WaitForEndOfFrame(); //Just slow down the number of updates
			}
		}

		public void TryResearchTech()
		{
			server.TryResearchTechnology(technologyToUnlock);
		}
	}
}
