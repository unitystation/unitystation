using UI.Core.Net.Elements;
using UI.Core.NetUI;
using UnityEngine;
using Systems.Research.Data;
using Systems.Research;
using System.Collections;

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
			techWeb.ResearchTechology(technologyToUnlock);
		}
	}
}
