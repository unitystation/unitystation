using Mirror;
using UnityEngine;
using US13.Core.Addressables.Types;
using US13.Core.Camera;
using US13.Core.Utils;
using US13.Managers;
using US13.Player;
using US13.Systems.Inventory;
using US13.Tilemaps.Behaviours.Objects;
using Util;

namespace US13.Clothing.Eyewear
{
	public class ShaderWhileWorn : NetworkBehaviour, IItemInOutMovedPlayer, IClientSynchronisedEffect
	{
		private enum ShaderType
		{
			Noir,
			Glitch,
		}

		[SerializeField] private ShaderType shaderType;
		[SerializeField] private AddressableAudioSource toggleSound = null;

		private IClientSynchronisedEffect Preimplemented => this;

		[SyncVar(hook = nameof(SyncOnPlayer))] public uint OnBodyID;

		public uint OnPlayerID => OnBodyID;

		[SyncVar(hook = nameof(SyncShader))] [SerializeField]
		private bool isOn = false;

		private RegisterPlayer currentlyOn;
		public RegisterPlayer CurrentlyOn
		{
			get => currentlyOn;
			set
			{
				SyncShader(currentlyOn, value);
				currentlyOn = value;
			}
		}
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
		public void SyncShader(bool oldState, bool newState)
		{
			isOn = newState;
			if (Preimplemented.IsOnLocalPlayer == false) return;
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
			
			if(shaderType == ShaderType.Noir) effects.ToggleNoirEffectState(finalState);
			else if(shaderType == ShaderType.Glitch) effects.ToggleGlitchEffectState(finalState);

			if (PlayerManager.LocalPlayerScript == null) return;
			if (toggleSound == null) return;

			_ = SoundManager.PlayNetworkedAtPosAsync(toggleSound, PlayerManager.LocalPlayerScript.RegisterPlayer.WorldPositionServer);
		}
	}
}