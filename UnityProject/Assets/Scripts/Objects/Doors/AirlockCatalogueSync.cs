using System.Collections.Generic;
using Doors;
using Logs;
using Mirror;
using ScriptableObjects;
using UnityEngine;

namespace Objects.Doors
{
	public class AirlockCatalogueSync : NetworkBehaviour
	{
		[SerializeField] private DoorsSO paintOptions;
		[SerializeField] private DoorAnimatorV2 animatorV2;

		[SyncVar(hook = nameof(SyncAirlockSprites))] private int index = -1;

		private void Awake()
		{
			if (animatorV2 == null) Loggy.LogError("[Door] - AirlockCatalogueSync is not setup properly, issues may happen.");
			if (paintOptions == null) Loggy.LogError("[Door] - AirlockCatalogueSync has its DoorsSO null. issues may happen.");
		}

		private void SyncAirlockSprites(int oldValue, int newValue)
		{
			if(newValue == -1) return;
			UpdateCatalogue(newValue);
		}

		public void SetNewIndex(int newIndex)
		{
			index = newIndex;
		}

		private void UpdateCatalogue(int newVal)
		{
			if (index >= paintOptions.Doors.Count)
			{
				Loggy.LogError($"[AirlockCatalgueSync] - Index out of bounds! New Index is {newVal} and number of options are {paintOptions.Doors.Count}");
				return;
			}

			var option = paintOptions.Doors[newVal].GetComponent<DoorAnimatorV2>();
			ServerChangeDoorBase(animatorV2, option);
			ServerChangeOverlaySparks(animatorV2, option);
			ServerChangeOverlayLights(animatorV2, option);
			ServerChangeOverlayFill(animatorV2, option);
			ServerChangeOverlayWeld(animatorV2, option);
			ServerChangeOverlayHacking(animatorV2, option);
		}

		private void ServerChangeDoorBase(DoorAnimatorV2 paintableAirlock, DoorAnimatorV2 paintJob)
		{
			SpriteHandler airlockSprite = paintableAirlock.DoorBase.GetComponent<SpriteHandler>();
			SpriteHandler paintSprite = paintJob.DoorBase.GetComponent<SpriteHandler>();
			List<SpriteDataSO> spriteCatalog = paintSprite.GetSubCatalogue();
			ServerSetCatalogue(airlockSprite, spriteCatalog);
		}
		private void ServerChangeOverlaySparks(DoorAnimatorV2 paintableAirlock, DoorAnimatorV2 paintJob)
		{
			SpriteHandler airlockSprite = paintableAirlock.OverlaySparks.GetComponent<SpriteHandler>();
			SpriteHandler paintSprite = paintJob.OverlaySparks.GetComponent<SpriteHandler>();
			List<SpriteDataSO> spriteCatalog = paintSprite.GetSubCatalogue();
			ServerSetCatalogue(airlockSprite, spriteCatalog);
		}
		private void ServerChangeOverlayLights(DoorAnimatorV2 paintableAirlock, DoorAnimatorV2 paintJob)
		{
			SpriteHandler airlockSprite = paintableAirlock.OverlayLights.GetComponent<SpriteHandler>();
			SpriteHandler paintSprite = paintJob.OverlayLights.GetComponent<SpriteHandler>();
			List<SpriteDataSO> spriteCatalog = paintSprite.GetSubCatalogue();
			ServerSetCatalogue(airlockSprite, spriteCatalog);
		}
		private void ServerChangeOverlayFill(DoorAnimatorV2 paintableAirlock, DoorAnimatorV2 paintJob)
		{
			SpriteHandler airlockSprite = paintableAirlock.OverlayFill.GetComponent<SpriteHandler>();
			SpriteHandler paintSprite = paintJob.OverlayFill.GetComponent<SpriteHandler>();
			List<SpriteDataSO> spriteCatalog = paintSprite.GetSubCatalogue();
			ServerSetCatalogue(airlockSprite, spriteCatalog);
		}
		private void ServerChangeOverlayWeld(DoorAnimatorV2 paintableAirlock, DoorAnimatorV2 paintJob)
		{
			SpriteHandler airlockSprite = paintableAirlock.OverlayWeld.GetComponent<SpriteHandler>();
			SpriteHandler paintSprite = paintJob.OverlayWeld.GetComponent<SpriteHandler>();
			List<SpriteDataSO> spriteCatalog = paintSprite.GetSubCatalogue();
			ServerSetCatalogue(airlockSprite, spriteCatalog);
		}
		private void ServerChangeOverlayHacking(DoorAnimatorV2 paintableAirlock, DoorAnimatorV2 paintJob)
		{
			SpriteHandler airlockSprite = paintableAirlock.OverlayHacking.GetComponent<SpriteHandler>();
			SpriteHandler paintSprite = paintJob.OverlayHacking.GetComponent<SpriteHandler>();
			List<SpriteDataSO> spriteCatalog = paintSprite.GetSubCatalogue();
			ServerSetCatalogue(airlockSprite, spriteCatalog);
		}
		private void ServerSetCatalogue(SpriteHandler airlockSprite, List<SpriteDataSO> spriteCatalog)
		{
			airlockSprite.SetCatalogue(spriteCatalog, 0);
			airlockSprite.SetSpriteSO(spriteCatalog[0]);    //For update the sprite when re-painting
		}
	}
}