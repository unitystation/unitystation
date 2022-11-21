using System;
using System.Collections.Generic;
using CameraEffects;
using Mirror;
using UI.Action;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Clothing
{
	public class NightVisionGoggles : NetworkBehaviour, IItemInOutMovedPlayer,
		ICheckedInteractable<HandActivate>, IClientSynchronisedEffect, IHoverTooltip
	{
		private static readonly float defaultvisibilityAnimationSpeed = 0.85f;
		private static readonly float revertvisibilityAnimationSpeed = 0.2f;
		private static readonly Vector3 expandedNightVisionVisibility = new(25, 25, 42);

		private IClientSynchronisedEffect Preimplemented => this;

		[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

		public uint OnPlayerID => OnBodyID;

		[SyncVar(hook = nameof(SyncNightVision))] [SerializeField]
		private bool isOn = false;

		public RegisterPlayer CurrentlyOn { get; set; }
		bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

		private ItemActionButton actionButton;
		private Pickupable pickupable;

		#region LifeCycle

		private void Awake()
		{
			actionButton = GetComponent<ItemActionButton>();
			pickupable = GetComponent<Pickupable>();
		}

		private void OnEnable()
		{
			// Subscribes to UI action buttons.
			actionButton.ServerActionClicked += ToggleGoggles;
		}

		private void OnDisable()
		{
			actionButton.ServerActionClicked -= ToggleGoggles;
		}

		#endregion

		#region InventoryMove

		public bool IsValidSetup(RegisterPlayer player)
		{
			if (player == null) return false;
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
			SetGoggleState(!isOn);
		}

		#endregion

		[Server]
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
			isOn = newState;
			// Checks to see if this item is on a player that's online.
			if (CurrentlyOn == null || CurrentlyOn.PlayerScript.connectionToClient == null) return;
			if (IsValidSetup(CurrentlyOn))
			{
				// Gives feedback to the player's actions.
				Chat.AddExamineMsg(CurrentlyOn.PlayerScript.gameObject,
					$"You turned {(isOn ? "on" : "off")} the {gameObject.ExpensiveName()}.");
			}
		}

		public void SyncOnPlayer(uint PreviouslyOn, uint CurrentlyOn)
		{
			OnBodyID = CurrentlyOn;
			Preimplemented.ImplementationSyncOnPlayer(PreviouslyOn, CurrentlyOn);
		}

		public void ApplyDefaultOrCurrentValues(bool def)
		{
			// (Max): I have no idea what "def" or "Default" means because the person who wrote this code
			// didn't bother to document their shit or give this value a proper name that's easy to understand,
			// I'm not obligated to do that job for them especially after they left this script in such a horrible state.
			ApplyEffects(def);
		}

		/// <summary>
		/// will always update the effects on the client whenever isOn has changed.
		/// </summary>
		public void SyncNightVision(bool oldState, bool newState)
		{
			isOn = newState;
			// Makes sure that the goggles are on the player before applying the effect.
			// If it's not on the player, ensure that the effect is disabled to avoid bugs when removing the goggles.
			ApplyEffects(Preimplemented.IsOnLocalPlayer && newState);
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
				finalState ? expandedNightVisionVisibility : effects.MinimalVisibilityScale,
				finalState ? defaultvisibilityAnimationSpeed : revertvisibilityAnimationSpeed);
			effects.ToggleNightVisionEffectState(finalState);
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
			throw new NotImplementedException();
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