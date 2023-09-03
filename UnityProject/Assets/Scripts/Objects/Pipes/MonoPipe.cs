using System.Collections.Generic;
using UnityEngine;
using Core.Editor.Attributes;
using Systems.Interaction;
using Systems.Pipes;
using Items.Atmospherics;
using Logs;
using Objects.Other;
using Systems.Atmospherics;
using Systems.Disposals;


namespace Objects.Atmospherics
{
	public class MonoPipe : MonoBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>, ICheckedInteractable<AiActivate>
	{

		public SpriteHandler spritehandler;

		public GameObject SpawnOnDeconstruct;

		public RegisterTile registerTile;
		public PipeData pipeData;
		public Matrix Matrix => registerTile.Matrix;
		public Vector3Int MatrixPos => registerTile.LocalPosition;

		public Color Colour = Color.white;

		[SerializeField]
		private bool spawnedFromItem = true;
		public bool SpawnedFromItem => spawnedFromItem;

		public Rotatable directional;

		public static float MaxInternalPressure { get; } = AtmosConstants.ONE_ATMOSPHERE * 50;

		#region Lifecycle

		public virtual void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			directional = GetComponent<Rotatable>();
		}

		public virtual void OnSpawnServer(SpawnInfo info)
		{
			//Only run SetUpPipes for mapped, otherwise the item being used to place it will have the wrong pipe data
			//As the pipe will not be rotated correctly before setup
			SetUpPipes(spawnedFromItem && info.SpawnType != SpawnType.Mapped);
		}

		public void SetUpPipes(bool DoNotSetRotation = false)
		{
			if (pipeData.PipeAction == null)
			{
				pipeData.PipeAction = new MonoActions();
			}
			registerTile.SetPipeData(pipeData);
			pipeData.MonoPipe = this;
			if (DoNotSetRotation == false)
			{
				int Offset = PipeFunctions.GetOffsetAngle(transform.localRotation.eulerAngles.z);
				pipeData.Connections.Rotate(Offset);
			}


			pipeData.OnEnable();
			spritehandler.OrNull()?.gameObject.OrNull()?.SetActive( true);
			spritehandler.OrNull()?.SetColor(Colour);
		}

		/// <summary>
		/// Is the function to denote that it will be pooled or destroyed immediately after this function is finished.
		/// Used for cleaning up anything that needs to be cleaned up before this happens.
		/// </summary>
		public virtual void OnDespawnServer(DespawnInfo info)
		{
			pipeData.OnDisable();
		}

		#endregion

		public virtual void TickUpdate() { }

		#region Interaction

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;

			return true;
		}

		public virtual void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			{
				TryUnwrench(interaction);
				return;
			}

			HandApplyInteraction(interaction);
		}

		// TODO: Share with pipe tile deconstruction script
		private void TryUnwrench(HandApply interaction)
		{
			if (registerTile.TileChangeManager.MetaTileMap.HasTile(registerTile.LocalPositionServer, LayerType.Floors))
			{
				Chat.AddExamineMsg(
						interaction.Performer,
						$"The floor plating must be exposed before you can disconnect the {gameObject.ExpensiveName()}!");
				return;
			}

			// Dangerous pipe pressure
			if (pipeData.mixAndVolume.GetGasMix().Pressure > AtmosConstants.ONE_ATMOSPHERE * 20)
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
						$"As you begin disconnecting the {gameObject.ExpensiveName()}, " +
								"a jet of gas blasts into your face... maybe you should reconsider?",
						string.Empty,
						string.Empty, // $"The pressure sends you flying!"
						string.Empty, // $"{interaction.Performer.ExpensiveName() is sent flying by pressure!"
						() => {
							Unwrench(interaction);
							// TODO: Knock performer around.
						});
			}
			else
			{
				ToolUtils.ServerPlayToolSound(interaction);
				Unwrench(interaction);
			}
		}

		private void Unwrench(HandApply interaction)
		{
			if (SpawnOnDeconstruct == null)
			{
				Loggy.LogError($"{this} is missing reference to {nameof(SpawnOnDeconstruct)}!", Category.Interaction);
				return;
			}

			var spawn = Spawn.ServerPrefab(SpawnOnDeconstruct, registerTile.WorldPositionServer, localRotation: directional.ByDegreesToQuaternion(directional.CurrentDirection));
			var PipeItem = spawn.GameObject.GetComponent<PipeItem>();
			PipeItem.rotatable.FaceDirection(directional.CurrentDirection);
			PipeItem.SetColour(Colour);

			OnDisassembly(interaction);
			pipeData.OnDisable();
			_ = Despawn.ServerSingle(gameObject);
		}

		public virtual void HandApplyInteraction(HandApply interaction) { }

		public virtual void OnDisassembly(HandApply interaction) { }

		//Ai interaction
		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			//Only alt and normal are used so dont need to check others, change if needed in the future
			if (interaction.ClickType != AiActivate.ClickTypes.NormalClick &&
			    interaction.ClickType != AiActivate.ClickTypes.AltClick) return false;

			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			AiInteraction(interaction);
		}

		public virtual void AiInteraction(AiActivate interaction) { }

		#endregion

		public void SetColour(Color newColour)
		{
			Colour = newColour;
		}

		#region Vent Crawl

		protected void DoVentCrawl(HandApply interaction, GasMix pipeMix)
		{
			Chat.AddActionMsgToChat(gameObject, $"You start to enter the {gameObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()} starts to enter the {gameObject.ExpensiveName()}!");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Escape);

			StandardProgressAction.Create(
				cfg,
				() => FinishEnteringPipe(interaction, pipeMix)
			).ServerStartProgress(ActionTarget.Object(registerTile), 5, interaction.Performer);
		}

		private void FinishEnteringPipe(HandApply interaction, GasMix pipeMix)
		{
			var container = Spawn.ServerPrefab(DisposalsManager.Instance.CrawlingVirtualContainerPrefab, registerTile.ObjectPhysics.Component.OfficialPosition);
			if (container.Successful == false)
			{
				Loggy.LogError("Failed to spawn crawling container!");
				return;
			}

			if (container.GameObject.TryGetComponent<CrawlingVirtualContainer>(out var virtualContainer) == false)
			{
				Loggy.LogError("Failed to find CrawlingVirtualContainer script!");
				return;
			}

			virtualContainer.Setup(pipeData, interaction.PerformerPlayerScript.RegisterPlayer);

			// Transfer contents
			if (container.GameObject.TryGetComponent<ObjectContainer>(out var objectContainer))
			{
				objectContainer.StoreObject(interaction.Performer);
			}
			if (container.GameObject.TryGetComponent<GasContainer>(out var gasContainer))
			{
				GasMix.TransferGas(gasContainer.GasMix, pipeMix, pipeMix.Moles);
			}

			Chat.AddActionMsgToChat(gameObject, $"You enter the {gameObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()} enters the {gameObject.ExpensiveName()}!");
		}

		#endregion

		#region Editor

		private void OnDrawGizmos()
		{
			var density = pipeData.mixAndVolume.Density();

			Gizmos.color = Color.white;
			DebugGizmoUtils.DrawText(density.ToString(), transform.position, 10);
			Gizmos.color = Color.magenta;

			Connections InCopy = pipeData.Connections;

			if (Application.isPlaying == false)
			{
				InCopy = pipeData.Connections.Copy();
				int offset = PipeFunctions.GetOffsetAngle(transform.localEulerAngles.z);
				InCopy.Rotate(offset);
			}

			if (InCopy.Directions[0].Bool)
			{
				var Toues = transform.position;
				Toues.y += 0.25f;
				Gizmos.DrawCube(Toues, Vector3.one*0.08f );
			}

			if (InCopy.Directions[1].Bool)
			{
				var Toues = transform.position;
				Toues.x += 0.25f;
				Gizmos.DrawCube(Toues, Vector3.one*0.08f );
			}

			if (InCopy.Directions[2].Bool)
			{
				var Toues = transform.position;
				Toues.y += -0.25f;
				Gizmos.DrawCube(Toues, Vector3.one*0.08f );
			}

			if (InCopy.Directions[3].Bool)
			{

				var Toues = transform.position;
				Toues.x += -0.25f;
				Gizmos.DrawCube(Toues, Vector3.one*0.08f );
			}
		}

		#endregion
	}
}
