using System;
using CameraEffects;
using Mirror;
using UI.Action;
using UnityEngine;

namespace Clothing
{
	public class NightVisionGoggles : NetworkBehaviour, IItemInOutMovedPlayer,
		ICheckedInteractable<HandActivate>, IClientSynchronisedEffect
	{
		private static readonly float defaultvisibilityAnimationSpeed = 0.85f;
		private static readonly Vector3 expandedNightVisionVisibility = new(25, 25, 42);
		private static readonly Vector3 normalNightVisionVisibility = new(3.5f, 3.5f, 8);

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
			if (player != null && player.PlayerScript.RegisterPlayer == pickupable.ItemSlot.Player &&
			    pickupable.ItemSlot is { NamedSlot: NamedSlot.eyes })
				return true;

			return false;
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
			// If for whatever reason unity is unable to catch the correct main camera that has the CameraEffectControlScript
			// Don't do anything.
			if (Camera.main == null ||
			    Camera.main.TryGetComponent<CameraEffectControlScript>(out var effects) == false) return;
			// Visibility is updated based on the on/off state of the goggles.
			// True means its on and will show an expanded view in the dark by changing the player's light view.
			// False will revert it to default.
			// (Max): Note that there is no "easy way" to grab the default values from the player prefab without writing ugly code
			// without starting to mention edge case scenarios where there are multiple cameras and minds in effect
			// So for now we're just using numbers that are used on all player prefabs we already use currently.
			// We can worry about making those values dynamic later when a prefab actually needs to use a different default value.
			effects.AdjustPlayerVisibility(
				state ? expandedNightVisionVisibility : normalNightVisionVisibility,
				state ? defaultvisibilityAnimationSpeed : 0.1f);
			effects.ToggleNightVisionEffectState(state);
		}
	}
}