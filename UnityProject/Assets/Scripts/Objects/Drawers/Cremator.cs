using System.Collections;
using System.Linq;
using UnityEngine;
using AddressableReferences;
using HealthV2;
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

		private ClearanceRestricted clearanceRestricted;

		private const float BURNING_DURATION = 5f;

		[SerializeField] private float burningDamage = 25f;

		protected override void Awake()
		{
			base.Awake();
			clearanceRestricted = GetComponent<ClearanceRestricted>();
		}

		// This region (Interaction-RightClick) shouldn't exist once TODO in class summary is done.
		#region Interaction-RightClick

		public RightClickableResult GenerateRightClickOptions()
		{
			RightClickableResult result = RightClickableResult.Create();
			if (drawerState == DrawerState.Open) return result;

			if (clearanceRestricted.HasClearance(PlayerManager.LocalPlayerObject) == false) return result;

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
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
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
				EntityTryEscape(interaction.Performer, null, MoveAction.NoMove);
				return;
			}
			if (interaction.IsAltClick && drawerState != DrawerState.Open)
			{
				Cremate();
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
				return;
			}
			SetDrawerState(DrawerState.Shut);
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
			foreach (var obj in container.GetStoredObjects())
			{
				if(obj.TryGetComponent<Integrity>(out var integrity)) //For items
					integrity.ApplyDamage(burningDamage, AttackType.Fire, DamageType.Burn, true);
				if (obj.TryGetComponent<LivingHealthBehaviour>(out var healthBehaviour)) //For NPCs
					healthBehaviour.ApplyDamage(gameObject, burningDamage, AttackType.Fire, DamageType.Burn);
				if (obj.TryGetComponent<PlayerHealthV2>(out var playerHealthV2)) //For Players
					playerHealthV2.ApplyDamageAll(gameObject, burningDamage, AttackType.Fire, DamageType.Burn, false, TraumaticDamageTypes.BURN);
			}

			yield return WaitFor.Seconds(BURNING_DURATION);
			//if it's just closed but not active don't start this again.
			if (drawerState == DrawerState.Shut || drawerState == DrawerState.Open) yield break;
			StartCoroutine(nameof(BurnContent));
		}

		private void OnStartPlayerCremation()
		{

			var objectsInContainer = container.GetStoredObjects();
			foreach (var player in objectsInContainer)
			{
				if (player.TryGetComponent<PlayerHealthV2>(out var healthBehaviour))
				{
					if(healthBehaviour.ConsciousState == ConsciousState.CONSCIOUS ||
					   healthBehaviour.ConsciousState == ConsciousState.BARELY_CONSCIOUS)
						EntityTryEscape(player, null, MoveAction.NoMove);
					// TODO: This is an incredibly brutal SFX... it also needs chopping up.
					// (Max): We should use the scream emote from the emote system when sounds are added for them
					// codacy ignore this ->SoundManager.PlayNetworkedAtPos("ShyguyScream", DrawerWorldPosition, sourceObj: gameObject);
				}
			}
		}

		#endregion
	}
}
