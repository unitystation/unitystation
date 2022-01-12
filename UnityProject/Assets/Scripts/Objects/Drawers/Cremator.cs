using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AddressableReferences;
using Items;
using Systems.Clearance;

namespace Objects.Drawers
{
	/// <summary>
	/// Cremator component for cremator objects, for use in crematorium rooms. Adds additional function to the base Drawer component.
	/// TODO: Implement activation via button when buttons can be assigned a generic component instead of only a DoorController component
	/// and remove the activation by right-click option.
	/// </summary>
	public class Cremator : Drawer, IRightClickable, ICheckedInteractable<ContextMenuApply>
	{
		[Tooltip("Sound used for cremation.")]
		[SerializeField] private AddressableAudioSource CremationSound = null;

		// Extra states over the base DrawerState enum.
		private enum CrematorState
		{
			/// <summary> Red light in red display. </summary>
			ShutWithContents = 2,
			/// <summary> Cremator is cremating. </summary>
			ShutAndActive = 3,
		}

		private AccessRestrictions accessRestrictions;
		private ClearanceCheckable clearanceCheckable;

		private const float BURNING_DURATION = 1.5f; // In seconds - timed to the Ding SFX.

		[SerializeField] private float burningDamage = 25f;

		protected override void Awake()
		{
			base.Awake();
			accessRestrictions = GetComponent<AccessRestrictions>();
			clearanceCheckable = GetComponent<ClearanceCheckable>();
		}

		// This region (Interaction-RightClick) shouldn't exist once TODO in class summary is done.
		#region Interaction-RightClick

		public RightClickableResult GenerateRightClickOptions()
		{
			RightClickableResult result = RightClickableResult.Create();
			if (drawerState == DrawerState.Open) return result;

			/* --ACCESS REWORK--
			 *  TODO Remove the AccessRestriction check when we finish migrating!
			 *
			 */

			if (accessRestrictions)
			{
				if (accessRestrictions.CheckAccess(PlayerManager.LocalPlayer) == false) return result;
			}
			else if (clearanceCheckable)
			{
				if (clearanceCheckable.HasClearance(PlayerManager.LocalPlayer) == false) return result;
			}

			var cremateInteraction = ContextMenuApply.ByLocalPlayer(gameObject, null);
			if (!WillInteract(cremateInteraction, NetworkSide.Client)) return result;

			return result.AddElement("Activate", () => OnCremateClicked(cremateInteraction));
		}

		private void OnCremateClicked(ContextMenuApply interaction)
		{
			InteractionUtils.RequestInteract(interaction, this);
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (drawerState == (DrawerState)CrematorState.ShutAndActive) return false;

			return true;
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			Cremate();
		}

		#endregion

		#region Interaction

		public override void ServerPerformInteraction(HandApply interaction)
		{
			if (drawerState == (DrawerState)CrematorState.ShutAndActive) return;
			if (container.GetStoredObjects().Contains(interaction.Performer))
			{
				Chat.AddExamineMsg(interaction.Performer, "<color=red>I can't reach the controls from the inside!</color>");
				EntityTryEscape(interaction.Performer, null);
				return;
			}
			base.ServerPerformInteraction(interaction);
		}

		#endregion

		#region Server Only

		public override void CloseDrawer()
		{
			base.CloseDrawer();
			// Note: the sprite setting done in base.CloseDrawer() would be overridden (an unnecessary sprite call).
			// "Not great, not terrible."

			UpdateCloseState();
		}

		public override void OpenDrawer()
		{
			base.OpenDrawer();
			if(drawerState == (DrawerState)CrematorState.ShutAndActive) StopCoroutine(BurnContent());
		}

		private void UpdateCloseState()
		{
			if (container.IsEmpty == false)
			{
				SetDrawerState((DrawerState)CrematorState.ShutWithContents);
			}
			else SetDrawerState(DrawerState.Shut);
		}

		private void Cremate()
		{
			SoundManager.PlayNetworkedAtPos(CremationSound, DrawerWorldPosition, sourceObj: gameObject);
			SetDrawerState((DrawerState)CrematorState.ShutAndActive);
			UpdateCloseState();
			OnStartPlayerCremation();
			StartCoroutine(nameof(BurnContent));
		}

		private IEnumerator BurnContent()
		{

			if (drawerState == (DrawerState)CrematorState.ShutAndActive)
			{
				foreach (var obj in container.GetStoredObjects())
				{
					if(obj.TryGetComponent<Integrity>(out var integrity))
						integrity.ApplyDamage(burningDamage, AttackType.Fire, DamageType.Burn, true);
					if(obj.TryGetComponent<LivingHealthBehaviour>(out var healthBehaviour))
						healthBehaviour.ApplyDamage(gameObject, burningDamage, AttackType.Fire, DamageType.Burn);
				}
				StartCoroutine(nameof(BurnContent));
			}

			yield return WaitFor.Seconds(5f);
		}

		private void OnStartPlayerCremation()
		{

			var objectsInContainer = container.GetStoredObjects();
			foreach (var player in objectsInContainer)
			{
				if (player.TryGetComponent<LivingHealthBehaviour>(out var healthBehaviour))
				{
					if(healthBehaviour.ConsciousState == ConsciousState.CONSCIOUS ||
					   healthBehaviour.ConsciousState == ConsciousState.BARELY_CONSCIOUS)
						EntityTryEscape(player, null);
					// TODO: This is an incredibly brutal SFX... it also needs chopping up.
					// (Max): We should use the scream emote from the emote system when sounds are added for them
					// codacy ignore this ->SoundManager.PlayNetworkedAtPos("ShyguyScream", DrawerWorldPosition, sourceObj: gameObject);
				}
			}
		}

		#endregion
	}
}
