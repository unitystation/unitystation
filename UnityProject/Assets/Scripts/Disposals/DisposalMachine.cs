using System.Collections;
using UnityEngine;
using Mirror;

namespace Disposals
{
	public enum InstallState
	{
		Unattached = 0, // default spawn state if no pipe terminal detected at spawn position,
						// sprite rotated, can push around, must anchor with wrench
		Anchored = 1, // can unanchor with wrench, can secure with welder
		Secured = 2, // ready for interaction, can open ui, can unsecure with welder, can connect with screwdriver, can start accepting items
	}

	public abstract class DisposalMachine : NetworkBehaviour, IExaminable, ICheckedInteractable<PositionalHandApply> // Must it be positional?
	{
		const float WELD_TIME = 2f;
		const string PIPE_TERMINAL_NAME = "disposal pipe terminal";

		protected RegisterObject registerObject;
		protected ObjectAttributes objectAttributes;
		protected ObjectBehaviour objectBehaviour;
		protected SpriteHandler baseSpriteHandler;

		protected PositionalHandApply currentInteraction;

		[SyncVar(hook = nameof(OnSyncInstallState))]
		protected InstallState installState = InstallState.Unattached;
		public bool MachineUnattached => installState == InstallState.Unattached;
		public bool MachineAnchored => installState == InstallState.Anchored;
		public bool MachineSecured => installState == InstallState.Secured;
		public bool MachineAttachedOrGreater => installState >= InstallState.Anchored;
		public virtual bool MachineWrenchable => MachineUnattached || MachineAnchored;
		public virtual bool MachineWeldable => MachineAnchored || MachineSecured;

		#region Initialisation

		protected virtual void Awake()
		{
			registerObject = GetComponent<RegisterObject>();
			objectAttributes = GetComponent<ObjectAttributes>();
			objectBehaviour = GetComponent<ObjectBehaviour>();

			baseSpriteHandler = transform.GetChild(0).GetComponent<SpriteHandler>();
		}

		public override void OnStartServer()
		{
			StartCoroutine(WaitForUnderfloorUtilities());
		}

		IEnumerator WaitForUnderfloorUtilities()
		{
			while (!registerObject.Matrix.UnderFloorLayer.UnderFloorUtilitiesInitialised)
			{
				yield return WaitFor.EndOfFrame;
			}

			if (PipeTerminalExists()) SpawnMachineAsInstalled();
			UpdateSpriteConstructionState();
		}

		/// <summary>
		/// Useful for mapping in disposal machine. Just place the machine over a disposal pipe terminal during mapping.
		/// </summary>
		protected virtual void SpawnMachineAsInstalled()
		{
			SetMachineInstalled();
			objectBehaviour.ServerSetPushable(false);
		}

		public override void OnStartClient()
		{
			UpdateSpriteConstructionState();
		}

		#endregion Initialisation

		#region Sync

		void OnSyncInstallState(InstallState oldState, InstallState newState)
		{
			installState = newState;
			UpdateSpriteConstructionState();
		}

		#endregion Sync

		#region Sprites

		protected virtual void UpdateSpriteConstructionState()
		{
			baseSpriteHandler.ChangeSprite(0);
		}

		#endregion Sprites

		#region Interactions

		public virtual bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.HandObject == null) return false;

			switch (installState)
			{
				case InstallState.Unattached:
					if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench)) return true;
					break;
				case InstallState.Anchored:
					if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench)) return true;
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

			if (Validations.HasUsedItemTrait(interaction, CommonTraits.Instance.Wrench)
					&& MachineWrenchable) TryUseWrench();
			else if (Validations.HasUsedActiveWelder(interaction)
					&& MachineWeldable) TryUseWelder();
		}

		public virtual string Examine(Vector3 worldPos = default)
		{
			if (MachineSecured) return $"It is welded securely to a {PIPE_TERMINAL_NAME}.";
			if (MachineAnchored) return $"It is bolted to a {PIPE_TERMINAL_NAME} but is not welded.";
			return $"It is not attached to a {PIPE_TERMINAL_NAME}.";
		}

		#endregion Interactions

		protected DisposalVirtualContainer SpawnNewContainer()
		{
			GameObject containerObject = DisposalsManager.SpawnVirtualContainer(registerObject.WorldPositionServer);
			containerObject.GetComponent<ObjectBehaviour>().parentContainer = objectBehaviour;
			return containerObject.GetComponent<DisposalVirtualContainer>();
		}

		#region Construction

		protected bool FloorPlatingExposed()
		{
			return !registerObject.TileChangeManager.MetaTileMap.HasTile(registerObject.LocalPositionServer, LayerType.Floors, true);
		}

		protected bool PipeTerminalExists()
		{
			foreach (DisposalPipe pipe in registerObject.Matrix.GetDisposalPipesAt(registerObject.LocalPositionServer))
			{
				if (pipe.PipeType == DisposalPipeType.Terminal) return true;
			}

			return false;
		}

		bool VerboseFloorExists()
		{
			if (!MatrixManager.IsSpaceAt(registerObject.WorldPositionServer, true)) return true;

			Chat.AddExamineMsg(currentInteraction.Performer, $"A floor must be present to secure the {objectAttributes.InitialName}!");
			return false;
		}

		bool VerbosePlatingExposed()
		{
			if (FloorPlatingExposed()) return true;

			Chat.AddExamineMsg(
					currentInteraction.Performer,
					$"The floor plating must be exposed before you can secure the {objectAttributes.InitialName} to the floor!");
			return false;
		}

		bool VerbosePipeTerminalExists()
		{
			if (PipeTerminalExists()) return true;

			Chat.AddExamineMsg(
					currentInteraction.Performer,
					$"The {objectAttributes.InitialName} needs a {PIPE_TERMINAL_NAME} underneath!");
			return false;
		}

		// Why just these protected? Move interactions here? Consider that?
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
				if (!VerboseFloorExists()) return;
				if (!VerbosePlatingExposed()) return;
				if (!VerbosePipeTerminalExists()) return;

				finishPerformerMsg = $"You bolt the {objectAttributes.InitialName} to the {PIPE_TERMINAL_NAME}.";
				finishOthersMsg = $"{currentInteraction.Performer.ExpensiveName()} bolts the " +
							$"{objectAttributes.InitialName} to the {PIPE_TERMINAL_NAME}.";
			}

			ToolUtils.ServerUseToolWithActionMessages(currentInteraction, 0, "", "", finishPerformerMsg, finishOthersMsg, () => UseWrench());
		}

		void UseWrench()
		{
			//objectBehaviour.ServerSetAnchored(!objectBehaviour.IsPushable, currentInteraction.Performer);
			objectBehaviour.ServerSetPushable(!objectBehaviour.IsPushable);
			if (objectBehaviour.IsPushable) installState = InstallState.Unattached;
			else installState = InstallState.Anchored;
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
				if (!VerboseFloorExists()) return;
				if (!VerbosePlatingExposed()) return;
				if (!VerbosePipeTerminalExists()) return;

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
					() => UseWelder()
			);
		}

		void UseWelder()
		{
			// Advance construction state
			if (MachineAnchored) SetMachineInstalled();
			// Retard construction state
			else if (MachineSecured) SetMachineUninstalled();
		}

		// Virtual, so specific machines can run logic when installation occurs.
		protected virtual void SetMachineInstalled()
		{
			installState = InstallState.Secured;
		}

		// Virtual, so specific machines can run logic when uninstallation occurs.
		protected virtual void SetMachineUninstalled()
		{
			installState = InstallState.Anchored;
		}

		#endregion Construction
	}
}
