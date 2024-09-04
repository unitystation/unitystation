using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AddressableReferences;
using Core.Admin.Logs;
using HealthV2;
using Objects.Production;
using Systems.Clearance;
using UI.Systems.Tooltips.HoverTooltips;
using Util.Independent.FluentRichText;

namespace Objects.Drawers
{
	/// <summary>
	/// Cremator component for cremator objects, for use in crematorium rooms. Adds additional function to the base Drawer component.
	/// and remove the activation by right-click option.
	/// </summary>
	[RequireComponent(typeof(BurningStorage))]
	public class Cremator : Drawer, IRightClickable, ICheckedInteractable<ContextMenuApply>, IHoverTooltip
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

		[SerializeField] private BurningStorage creamationStorage;

		protected override void Awake()
		{
			base.Awake();
			clearanceRestricted = GetComponent<ClearanceRestricted>();
			creamationStorage ??= GetComponent<BurningStorage>();
		}

		#region Interaction-RightClick

		public RightClickableResult GenerateRightClickOptions()
		{
			RightClickableResult result = RightClickableResult.Create();
			if (drawerState == DrawerState.Open) return result;

			if (clearanceRestricted.HasClearance(PlayerManager.LocalPlayerObject) == false) return result;

			var cremateInteraction = ContextMenuApply.ByLocalPlayer(gameObject, null);
			if (WillInteract(cremateInteraction, NetworkSide.Client) == false) return result;

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
				AdminLogsManager.AddNewLog(
					gameObject,
					$"{interaction.PerformerPlayerScript.playerName} has enabled a cremator at {gameObject.AssumedWorldPosServer()}",
					LogCategory.Interaction,
					Severity.SUSPICOUS
					);
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
			creamationStorage.TurnOff();
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
			PlayCremationSound();
			SetDrawerState((DrawerState)CrematorState.ShutAndActive);
			UpdateCloseState();
			OnStartPlayerCremation();
			creamationStorage.TurnOn();
		}

		public void PlayCremationSound()
		{
			SoundManager.PlayNetworkedAtPos(CremationSound, DrawerWorldPosition, sourceObj: gameObject);
		}

		private void OnStartPlayerCremation()
		{
			var objectsInContainer = container.GetStoredObjects();
			foreach (var player in objectsInContainer)
			{
				if (player.TryGetComponent<PlayerHealthV2>(out var healthBehaviour) == false) continue;
				if (healthBehaviour.ConsciousState is ConsciousState.CONSCIOUS)
				{
					EntityTryEscape(player, null, MoveAction.NoMove);
					healthBehaviour.IndicatePain();
				}
			}
		}
		#endregion

		public string HoverTip()
		{
			var status = creamationStorage.IsBurning ? "on".Color(Color.red) : "off".Color(Color.gray);
			return $"It is currently {status}";
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			var interactions = new List<TextColor>();
			interactions.Add(creamationStorage.IsBurning
				? new TextColor() { Color = Color.green, Text = $"Left Click to open and stop the cremation process." }
				: new TextColor() { Color = Color.green, Text = $"Left Click to open/close it." });
			if (drawerState == DrawerState.Shut && creamationStorage.IsBurning == false)
			{
				interactions.Add(new TextColor(){ Color = Color.green, Text = $"Alt + Left Click to quickly activate it."});
			}
			return interactions;
		}
	}
}