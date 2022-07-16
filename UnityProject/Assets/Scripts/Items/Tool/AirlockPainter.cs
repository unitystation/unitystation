using System;
using Items.Bureaucracy;
using Mirror;
using Objects.Construction;
using System.Collections.Generic;
using UI.Core.RightClick;
using UnityEngine;
using Util;

namespace Doors
{
	[RequireComponent(typeof(ItemStorage))]
	public class AirlockPainter : MonoBehaviour, IClientInteractable<HandActivate>, ICheckedInteractable<ContextMenuApply>, ICheckedInteractable<HandApply>,
		ICheckedInteractable<InventoryApply>, IExaminable, IServerSpawn, IRightClickable
	{
		private static readonly RadialScreenPosition RadialScreenPosition = new RadialScreenPosition(true);

		[SerializeField]
		private RightClickRadialOptions radialOptions;

		private RightClickRadialOptions RadialOptions =>
			this.VerifyNonChildReference(radialOptions, "right click branchless options SO");

		[Tooltip("Airlock painting jobs.")]
		public List<GameObject> AvailablePaintJobs;

		private List<RightClickMenuItem> painterMenuItems;

		private List<RightClickMenuItem> PainterMenuItems => painterMenuItems ??= GeneratePaintMenu(AvailablePaintJobs);

		private int currentPaintJobIndex = -1;
		public int CurrentPaintJobIndex
		{
			get => currentPaintJobIndex;
			set => currentPaintJobIndex = value;
		}

		[SerializeField, Tooltip("The toner prefab to be spawned within the airlock painter on roundstart.")]
		private GameObject tonerPrefab;

		private ItemSlot tonerSlot;

		private Toner TonerCartridge =>
			tonerSlot.Item != null ? tonerSlot.Item.GetComponent<Toner>() : null;

		private void Awake()
		{
			tonerSlot = GetComponent<ItemStorage>().GetIndexedItemSlot(0);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (tonerPrefab == null)
			{
				Logger.LogError($"{gameObject.name} toner prefab was null, cannot auto-populate.", Category.ItemSpawn);
				return;
			}
			Inventory.ServerSpawnPrefab(tonerPrefab, tonerSlot);
		}

		public void ChoosePaintJob()
		{
			if (AvailablePaintJobs is null || RadialOptions is null) return;

			var controller = RightClickManager.Instance.MenuController;
			controller.SetupMenu(PainterMenuItems, RadialScreenPosition, RadialOptions);
		}

		public List<RightClickMenuItem> GeneratePaintMenu(List<GameObject> objects)
		{
			if (objects is null || objects.Count == 0) return null;

			var result = new List<RightClickMenuItem>();

			for (var i = 0; i < objects.Count; i++)
			{
				var index = i; // Copy for action function
				Action setPaint = () => PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSetPaintJob(index);
				var res = RightClickManager.CreateObjectMenu(objects[i], null, setPaint);
				result.Add(res);
			}

			return result;
		}

		[Server]
		public void ServerPaintTheAirlock(GameObject paintableAirlock, GameObject performer)
		{
			if(currentPaintJobIndex == -1)
			{
				Chat.AddExamineMsgFromServer(performer, "First you need to choose a paint job.");
				return;
			}

			if (CheckToner(performer) == false) return;

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

			TonerCartridge.SpendInk();
		}

		#region Toner
		private bool CheckToner(GameObject performer)
		{
			if(TonerCartridge == null)
			{
				Chat.AddExamineMsgFromServer(performer, $"There is no toner cartridge installed in {gameObject.ExpensiveName()}!");
				return false;
			}
			if (TonerCartridge.CheckInkLevel() == false)
			{
				Chat.AddExamineMsgFromServer(performer, "The toner cartridge is out of ink!");
				return false;
			}
			return true;
		}

		private void EjectToner(Interaction interaction)
		{
			if (tonerSlot.Item == null) return;

			ItemSlot activeHand = interaction.PerformerPlayerScript.DynamicItemStorage.GetActiveHandSlot();
			if (activeHand.IsEmpty)
			{
				Inventory.ServerTransfer(tonerSlot, activeHand);
			}
			else
			{
				Inventory.ServerDrop(tonerSlot);
			}
		}

		private void InsertToner(GameObject performer, ItemSlot tonerSlot)
		{
			if (this.tonerSlot.IsEmpty == false)
			{
				Chat.AddExamineMsgFromServer(performer, "Toner cartridge already installed.");
				return;
			}
			if (tonerSlot.IsEmpty) return;
			Inventory.ServerTransfer(tonerSlot, this.tonerSlot);
		}
		#endregion

		#region Right Click Menu
		public RightClickableResult GenerateRightClickOptions()
		{
			var result = RightClickableResult.Create();

			var ejectInteraction = ContextMenuApply.ByLocalPlayer(gameObject, null);
			result.AddElement("Eject Toner", () => ContextMenuOptionClicked(ejectInteraction));

			return result;
		}

		private void ContextMenuOptionClicked(ContextMenuApply interaction)
		{
			InteractionUtils.RequestInteract(interaction, this);
		}
		#endregion

		#region Interactions
		public bool Interact(HandActivate interaction)
		{
			ChoosePaintJob();
			return true;
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			EjectToner(interaction);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (!Validations.IsTarget(gameObject, interaction)) return false;

			if (Validations.HasUsedComponent<Toner>(interaction))
				return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasUsedComponent<Toner>(interaction))
			{
				InsertToner(interaction.Performer, interaction.HandSlot);
			}
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			//only works in hands
			return interaction.IsFromHandSlot && interaction.IsToHandSlot;
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if(interaction.UsedObject == null)
			{
				EjectToner(interaction);
				return;
			}
			if (Validations.HasUsedComponent<Toner>(interaction))
			{
				ItemSlot activeHand = interaction.PerformerPlayerScript.DynamicItemStorage.GetActiveHandSlot();
				InsertToner(interaction.Performer, activeHand);
			}
		}
		#endregion

		#region Airlock sprites changes
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
		#endregion

		public string Examine(Vector3 worldPos)
		{
			string msg = "";

			if (currentPaintJobIndex == -1)
			{
				msg += "Paint job is not selected.\n"; 
			}
			else
			{
				msg += $"Current paint job is the {AvailablePaintJobs[currentPaintJobIndex].ExpensiveName()}.\n";
			}

			if (TonerCartridge == null)
			{
				msg += "Toner cartridge not installed.\n";
			}
			else
			{
				msg += TonerCartridge.InkLevel();
			}
			return msg;
		}
	}
}

