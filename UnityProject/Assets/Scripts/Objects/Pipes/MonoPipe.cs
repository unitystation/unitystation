using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class MonoPipe : MonoBehaviour, IServerLifecycle, ICheckedInteractable<HandApply>
	{
		public SpriteHandler spritehandler;
		public GameObject SpawnOnDeconstruct;
		public RegisterTile registerTile;
		public PipeData pipeData;
		public Matrix Matrix => registerTile.Matrix;
		public Vector3Int MatrixPos => registerTile.LocalPosition;

		public Color Colour = Color.white;

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
		}

		public virtual void OnSpawnServer(SpawnInfo info)
		{
			if (pipeData.PipeAction == null)
			{
				pipeData.PipeAction = new MonoActions();
			}
			registerTile.SetPipeData(pipeData);
			pipeData.MonoPipe = this;
			int Offset = PipeFunctions.GetOffsetAngle(transform.localRotation.eulerAngles.z);
			pipeData.Connections.Rotate(Offset);
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
			if (SpawnOnDeconstruct != null)
			{
				if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
				{
					ToolUtils.ServerPlayToolSound(interaction);
					var Item = Spawn.ServerPrefab(SpawnOnDeconstruct, registerTile.WorldPositionServer, localRotation: this.transform.localRotation);
					Item.GameObject.GetComponent<PipeItem>().SetColour(Colour);
					OnDisassembly(interaction);
					pipeData.OnDisable();
					_ = Despawn.ServerSingle(gameObject);
					return;
				}
			}

			Interaction(interaction);
		}

		public virtual void Interaction(HandApply interaction) { }

		public virtual void OnDisassembly(HandApply interaction) { }

		#endregion

		public void SetColour(Color newColour)
		{
			Colour = newColour;
		}

		#region Editor

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.white;
			DebugGizmoUtils.DrawText(pipeData.mixAndVolume.Density().ToString(), transform.position, 10);
			Gizmos.color = Color.magenta;
			if (pipeData.Connections.Directions[0].Bool)
			{
				var Toues = transform.position;
				Toues.y += 0.25f;
				Gizmos.DrawCube(Toues, Vector3.one*0.08f );
			}

			if (pipeData.Connections.Directions[1].Bool)
			{
				var Toues = transform.position;
				Toues.x += 0.25f;
				Gizmos.DrawCube(Toues, Vector3.one*0.08f );
			}

			if (pipeData.Connections.Directions[2].Bool)
			{
				var Toues = transform.position;
				Toues.y += -0.25f;
				Gizmos.DrawCube(Toues, Vector3.one*0.08f );
			}

			if (pipeData.Connections.Directions[3].Bool)
			{

				var Toues = transform.position;
				Toues.x += -0.25f;
				Gizmos.DrawCube(Toues, Vector3.one*0.08f );
			}
		}

		#endregion
	}
}
