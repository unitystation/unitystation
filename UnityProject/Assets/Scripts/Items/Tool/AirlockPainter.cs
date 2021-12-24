using Objects.Construction;
using System.Collections.Generic;
using UnityEngine;

namespace Doors
{
	public class AirlockPainter : MonoBehaviour, IClientInteractable<HandActivate>
	{
		[Tooltip("Airlock painting jobs.")]
		public List<GameObject> AvailablePaintJobs;

		private int currentPaintJobIndex = -1;

		public int CurrentPaintJobIndex
		{
			get => currentPaintJobIndex;
			set => currentPaintJobIndex = value;
		}

		public bool Interact(HandActivate interaction)
		{
			ChoosePainJob(interaction.Performer);
			return true;
		}

		public async void ChoosePainJob(GameObject performer)
		{
			if (AvailablePaintJobs == null) return;

			GameObject chosenPaintJob = await UIManager.RadialMenu.ShowRadialMenu(AvailablePaintJobs, performer);
			if (chosenPaintJob)
			{
				int chosenPaintJobIndex = AvailablePaintJobs.IndexOf(chosenPaintJob);
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetPaintJob(chosenPaintJobIndex);
			}
		}

		public void ServerPaintTheAirlock(GameObject paintableAirlock, GameObject performer)
		{
			if(currentPaintJobIndex == -1)
			{
				Chat.AddExamineMsgFromServer(performer, "First you need to choose a paint job.");
				return;
			}
			DoorMasterController airlockToPaint = paintableAirlock.GetComponent<DoorMasterController>();
			GameObject airlockAssemblyPrefab = AvailablePaintJobs[currentPaintJobIndex].GetComponent<ConstructibleDoor>().AirlockAssemblyPrefab;
			AirlockAssembly assemblyPaintJob = airlockAssemblyPrefab.GetComponent<AirlockAssembly>();
			DoorAnimatorV2 paintJob = assemblyPaintJob.AirlockToSpawn.GetComponent<DoorAnimatorV2>();

			if (airlockToPaint.isWindowedDoor)
			{
				paintJob = assemblyPaintJob.AirlockWindowedToSpawn.GetComponent<DoorAnimatorV2>();
				if (paintJob == null)
				{
					Chat.AddExamineMsgFromServer(performer, "Selected paint job doesn't support windowed airlocks.");
					return;
				}
			}

			DoorAnimatorV2 airlockAnim = paintableAirlock.GetComponent<DoorAnimatorV2>();

			ServerChangeDoorBase(airlockAnim, paintJob);
			ServerChangeOverlaySparks(airlockAnim, paintJob);
			ServerChangeOverlayLights(airlockAnim, paintJob);
			ServerChangeOverlayFill(airlockAnim, paintJob);
			ServerChangeOverlayWeld(airlockAnim, paintJob);
			ServerChangeOverlayHacking(airlockAnim, paintJob);
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

