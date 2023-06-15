using UnityEngine;
using Mirror;
using Objects.Atmospherics;

namespace Objects.Disposals
{
	public enum InstallState
	{
		/// <summary>
		/// Sprite rotated, can push around, must anchor with wrench.
		/// Default spawn state if no pipe terminal detected at spawn position.
		/// </summary>
		Unattached = 0,
		/// <summary> Can unanchor with wrench, can secure with welder. </summary>
		Anchored = 1,
		/// <summary>
		/// Ready for interaction: can open UI, unsecure with welder, connect with screwdriver and accept items.
		/// </summary>
		Secured = 2
	}

	public abstract class DisposalMachine : NetworkBehaviour, IServerSpawn, IExaminable, ICheckedInteractable<PositionalHandApply> // Must it be positional?
	{
		private const float WELD_TIME = 2f;
		private const string PIPE_TERMINAL_NAME = "disposal pipe terminal";

		protected RegisterObject registerObject;
		protected ObjectAttributes objectAttributes;
		protected UniversalObjectPhysics objectPhysics;
		protected ObjectContainer objectContainer;
		protected GasContainer gasContainer;
		protected SpriteHandler baseSpriteHandler;

		protected PositionalHandApply currentInteraction;

		[SyncVar]
		private InstallState installState = InstallState.Unattached;
		public bool MachineUnattached => installState == InstallState.Unattached;
		public bool MachineAnchored => installState == InstallState.Anchored;
		public bool MachineSecured => installState == InstallState.Secured;
		public bool MachineAttachedOrGreater => installState >= InstallState.Anchored;
		public virtual bool MachineWrenchable => MachineUnattached || MachineAnchored;
		public virtual bool MachineWeldable => MachineAnchored || MachineSecured;

		#region Lifecycle

		protected virtual void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			objectAttributes = GetComponent<ObjectAttributes>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
			objectContainer = GetComponent<ObjectContainer>();
			gasContainer = GetComponent<GasContainer>();

			baseSpriteHandler = transform.GetChild(0).GetComponent<SpriteHandler>();
		}

		public virtual void OnSpawnServer(SpawnInfo info)
		{
			if (PipeTerminalExists())
			{
				SpawnMachineAsInstalled();
			}
		}

		/// <summary>
		/// Useful for mapping in disposal machine. Just place the machine over a disposal pipe terminal during mapping.
		/// </summary>
		protected virtual void SpawnMachineAsInstalled()
		{
			SetMachineInstalled();
			objectPhysics.SetIsNotPushable(true);
		}

		#endregion Lifecycle

		#region Sprites

		protected virtual void UpdateSpriteConstructionState()
		{
			baseSpriteHandler.ChangeSprite(0);
		}

		#endregion Sprites

		#region Interactions

		public virtual bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.HandObject == null) return false;

			switch (installState)
			{
				case InstallState.Unattached:
					if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench)) return true;
					break;
				case InstallState.Anchored:
					if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench)) return true;
					if (Validations.HasUsedActiveWelder(interaction)) return true;
					break;
				case InstallState.Secured:
					if (Validations.HasUsedActiveWelder(interaction)) return true;
					break;
			}

			return false;
		}

		public virtual void ServerPerformInteraction(PositionalHandApply interaction)
		{
			currentInteraction = interaction;

			if (Validations.HasItemTrait(interaction, CommonTraits.Instance.Wrench) && MachineWrenchable)
			{
				TryUseWrench();
			}
			else if (Validations.HasUsedActiveWelder(interaction) && MachineWeldable)
			{
				TryUseWelder();
			}
		}

		public virtual string Examine(Vector3 worldPos = default)
		{
			if (MachineSecured)
			{
				return $"It is welded securely to a {PIPE_TERMINAL_NAME}.";
			}
			if (MachineAnchored)
			{
				return $"It is bolted to a {PIPE_TERMINAL_NAME} but is not welded.";
			}
			return $"It is not attached to a {PIPE_TERMINAL_NAME}.";
		}

		#endregion Interactions

		#region Construction

		protected bool FloorPlatingExposed()
		{
			return registerObject.TileChangeManager.MetaTileMap.HasTile(registerObject.LocalPositionServer, LayerType.Floors) == false;
		}

		protected bool PipeTerminalExists()
		{
			foreach (DisposalPipe pipe in registerObject.Matrix.GetDisposalPipesAt(registerObject.LocalPositionServer))
			{
				if (pipe.PipeType == DisposalPipeType.Terminal) return true;
			}

			return false;
		}

		private bool VerboseFloorExists()
		{
			if (MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true, registerObject.Matrix.MatrixInfo) == false) return true;

			Chat.AddExamineMsg(currentInteraction.Performer, $"A floor must be present to secure the {objectAttributes.InitialName}!");
			return false;
		}

		private bool VerbosePlatingExposed()
		{
			if (FloorPlatingExposed()) return true;

			Chat.AddExamineMsg(
					currentInteraction.Performer,
					$"The floor plating must be exposed before you can secure the {objectAttributes.InitialName} to the floor!");
			return false;
		}

		private bool VerbosePipeTerminalExists()
		{
			if (PipeTerminalExists()) return true;

			Chat.AddExamineMsg(
					currentInteraction.Performer,
					$"The {objectAttributes.InitialName} needs a {PIPE_TERMINAL_NAME} underneath!");
			return false;
		}

		protected void TryUseWrench()
		{
			string finishPerformerMsg, finishOthersMsg;

			if (MachineAnchored)
			{
				finishPerformerMsg = $"You unbolt the {objectAttributes.InitialName} from the {PIPE_TERMINAL_NAME}.";
				finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} unbolts the " +
						$"{objectAttributes.InitialName} from the {PIPE_TERMINAL_NAME}.";
			}
			else
			{
				if (VerboseFloorExists() == false) return;
				if (VerbosePlatingExposed() == false) return;
				if (VerbosePipeTerminalExists() == false) return;

				finishPerformerMsg = $"You bolt the {objectAttributes.InitialName} to the {PIPE_TERMINAL_NAME}.";
				finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} bolts the " +
							$"{objectAttributes.InitialName} to the {PIPE_TERMINAL_NAME}.";
			}

			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, 0, "", "", finishPerformerMsg, finishOthersMsg, UseWrench);
		}

		private void UseWrench()
		{
			objectPhysics.SetIsNotPushable(!objectPhysics.IsNotPushable);

			if (objectPhysics.IsNotPushable == false)
			{
				SetInstallState(InstallState.Unattached);
			}
			else
			{
				SetInstallState(InstallState.Anchored);
			}
		}

		protected void TryUseWelder()
		{
			string startPerformerMsg, startOthersMsg, finishPerformerMsg, finishOthersMsg;

			if (MachineSecured)
			{
				startPerformerMsg = "You start cutting the welds between the " +
						$"{objectAttributes.InitialName} and the {PIPE_TERMINAL_NAME}...";
				startOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} starts cutting the " +
						$"{objectAttributes.InitialName} from the {PIPE_TERMINAL_NAME}...";
				finishPerformerMsg = $"You cut the {objectAttributes.InitialName} free from the {PIPE_TERMINAL_NAME}.";
				finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} cuts the " +
						$"{objectAttributes.InitialName} free from the {PIPE_TERMINAL_NAME}.";
			}
			else
			{
				if (VerboseFloorExists() == false) return;
				if (VerbosePlatingExposed() == false) return;
				if (VerbosePipeTerminalExists() == false) return;

				startPerformerMsg = "You start welding the joints between the " +
						$"{objectAttributes.InitialName} and the {PIPE_TERMINAL_NAME}...";
				startOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} starts welding the " +
						$"{objectAttributes.InitialName} and the {PIPE_TERMINAL_NAME} together...";
				finishPerformerMsg = $"You weld the {objectAttributes.InitialName} to the {PIPE_TERMINAL_NAME}.";
				finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} welds the " +
						$"{objectAttributes.InitialName} to the {PIPE_TERMINAL_NAME}.";
			}

			ToolUtils.ServerUseToolWithActionMessages(
					currentInteraction, WELD_TIME,
					startPerformerMsg, startOthersMsg, finishPerformerMsg, finishOthersMsg,
					 UseWelder
			);
		}

		private void UseWelder()
		{
			// Advance construction state
			if (MachineAnchored)
			{
				SetMachineInstalled();
			}
			// Retard construction state
			else if (MachineSecured)
			{
				SetMachineUninstalled();
			}
		}

		// Virtual, so specific machines can run logic when installation occurs.
		protected virtual void SetMachineInstalled()
		{
			SetInstallState(InstallState.Secured);
		}

		// Virtual, so specific machines can run logic when uninstallation occurs.
		protected virtual void SetMachineUninstalled()
		{
			SetInstallState(InstallState.Anchored);
		}

		private void SetInstallState(InstallState newInstallState)
		{
			installState = newInstallState;
			UpdateSpriteConstructionState();
		}

		#endregion Construction
	}
}
