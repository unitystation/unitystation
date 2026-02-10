using System.Collections.Generic;
using Mirror;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Camera;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Utils;
using US13.Items;
using US13.Managers;
using US13.Player;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using US13.UI.Systems.Tooltips.HoverTooltips;
using Util;

namespace US13.Clothing.Eyewear
{
	public class NightVisionGoggles : NetworkBehaviour, IItemInOutMovedPlayer,
		ICheckedInteractable<HandActivate>, IClientSynchronisedEffect, IHoverTooltip
	{
		private static readonly float DefaultvisibilityAnimationSpeed = 1.25f;
		private static readonly float RevertvisibilityAnimationSpeed = 0.2f;
		private static readonly Vector3 ExpandedNightVisionVisibility = new Vector3(25, 25, 42);

		[SerializeField] private float darknessVisibilityMultiplier = 15.0f;
		[SerializeField] private Color dimLightColour = new Color(255,255,255,10);
		[SerializeField] private Color shaderColour = new Color(26,255,26, 255);

		[SerializeField] private AddressableAudioSource nightVisionToggleSound;

		private IClientSynchronisedEffect Preimplemented => this;

		[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

		public uint OnPlayerID => OnBodyID;

		[SyncVar(hook = nameof(SyncNightVision))] [SerializeField]
		private bool isOn = false;

		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; } = false;

		private Pickupable pickupable;

		#region LifeCycle

		private void Awake()
		{
			pickupable = GetComponent<Pickupable>();
		}

		#endregion

		#region InventoryMove

		public bool IsValidSetup(RegisterPlayer player)
		{
			if (player == false) return false;
			// Checks if it's not null and checks if NamedSlot == NamedSlot.eyes
			return player.PlayerScript.RegisterPlayer == pickupable.ItemSlot.Player && IsInCorrectNamedSlot();
		}

		/// <summary>
		/// Checks if the item is in the correct ItemSlot which is the eyes.
		/// Automatically returns false if null because of the "is" keyword and null propagation.
		/// </summary>
		private bool IsInCorrectNamedSlot()
		{
			return pickupable.ItemSlot is { NamedSlot: NamedSlot.eyes };
		}

		void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer HideForPlayer, RegisterPlayer ShowForPlayer)
		{
			OnBodyID = ShowForPlayer != null ? ShowForPlayer.netId : NetId.Empty;
		}

		#endregion

		#region HandInteract

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			ToggleGoggles();
		}

		#endregion


		public void OnButtonPress(Vector2 mousePosition)
		{
			ToggleGoggles();
		}

		private void ToggleGoggles()
		{
			SetGoggleState(!isOn);
		}

		/// <summary>
		/// Turning goggles on or off
		/// </summary>
		/// <param name="newState"></param>
		[Server]
		private void SetGoggleState(bool newState)
		{
			// Checks to see if this item is on a player that's online.
			if (CurrentlyOn == null || CurrentlyOn.PlayerScript.connectionToClient == null) return;
			if (IsValidSetup(CurrentlyOn))
			{
				isOn = newState;
				// Gives feedback to the player's actions.
				Chat.AddExamineMsg(CurrentlyOn.PlayerScript.gameObject,
					$"You turned {(isOn ? "on" : "off")} the {gameObject.ExpensiveName()}.");
			}
		}

		/// <summary>
		/// Syncs the player body that this item is on.
		/// </summary>
		public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
		{
			OnBodyID = CurrentlyOn;
			Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
		}

		public void ApplyDefaultOrCurrentValues(bool Default)
		{
			// Inverse of default for correct state
			ApplyEffects(!Default && isOn);
		}

		/// <summary>
		/// will always update the effects on the client whenever isOn has changed.
		/// </summary>
		public void SyncNightVision(bool oldState, bool newState)
		{
			isOn = newState;
			if (Preimplemented.IsOnLocalPlayer == false) return;

			// Makes sure that the goggles are on the player before applying the effect.
			// If it's not on the player, ensure that the effect is disabled to avoid bugs when removing the goggles.
			ApplyEffects(newState);
		}

		private void ApplyEffects(bool state)
		{
			var finalState = state;

			// If for whatever reason unity is unable to catch the correct main camera that has the CameraEffectControlScript
			// Don't do anything.
			if (Camera.main == null ||
			    Camera.main.TryGetComponent<CameraEffectControlScript>(out var effects) == false) return;
			// If the item is not in the correct slot, ensure the effect is disabled.
			if (IsInCorrectNamedSlot() == false) finalState = false;
			// Visibility is updated based on the on/off state of the goggles.
			// True means its on and will show an expanded view in the dark by changing the player's light view.
			// False will revert it to default.
			effects.AdjustPlayerVisibility(
				finalState ? ExpandedNightVisionVisibility : effects.MinimalVisibilityScale,
				finalState ? DefaultvisibilityAnimationSpeed : RevertvisibilityAnimationSpeed);
			effects.ToggleNightVisionEffectState(finalState, shaderColour);

			if (PlayerManager.LocalPlayerScript == null) return;
			DimPlayerLightController dimLightController = PlayerManager.LocalPlayerScript.DimPlayerLightController;

			if (dimLightController != null && state)
			{
				dimLightController.lightColor = dimLightColour;
				dimLightController.UpdateLightData(DimPlayerLightController.DEFAULT_SIZE * darknessVisibilityMultiplier, true);
			}
			else if(dimLightController != null) dimLightController.ResetToDefault();

			_ = SoundManager.PlayNetworkedAtPosAsync(nightVisionToggleSound, PlayerManager.LocalPlayerScript.RegisterPlayer.WorldPositionServer);
		}

		#region Tooltip

		public string HoverTip()
		{
			return null;
		}

		public string CustomTitle()
		{
			if (gameObject.TryGetComponent<Attributes>(out var attributes) == false) return null;
			var state = isOn ? "On" : "Off";
			return $"{attributes.ArticleName} [{state}]";
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return new List<Sprite>();
		}

		public List<TextColor> InteractionsStrings()
		{
			TextColor inspectText = new TextColor
			{
				Text = "Left Click or Z: Turn On/Off.",
				Color = Color.green
			};

			List<TextColor> interactions = new List<TextColor>();
			interactions.Add(inspectText);
			return interactions;
		}

		#endregion

	}
}