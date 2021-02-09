using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class MonoPipe : MonoBehaviour, IServerDespawn, ICheckedInteractable<HandApply>
	{
		public SpriteHandler spritehandler;
		public GameObject SpawnOnDeconstruct;
		public RegisterTile registerTile;
		public PipeData pipeData;
		public Matrix Matrix => registerTile.Matrix;
		public Vector3Int MatrixPos => registerTile.LocalPosition;

		public Color Colour = Color.white;

		public virtual void Start()
		{
			EnsureInit();
		}

		public void SetColour(Color newColour)
		{
			Colour = newColour;
			spritehandler.SetColor(Colour);
		}

		private void EnsureInit()
		{
			if (registerTile == null)
			{
				registerTile = GetComponent<RegisterTile>();
			}

			registerTile.SetPipeData(pipeData);
			pipeData.MonoPipe = this;
			int Offset = PipeFunctions.GetOffsetAngle(this.transform.localRotation.eulerAngles.z);
			pipeData.Connections.Rotate(Offset);
			pipeData.OnEnable();
			spritehandler?.SetColor(Colour);
		}

		void OnEnable()
		{

		}

		public virtual void TickUpdate()
		{
		}
		/// <summary>
		/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
		/// </summary>
		public virtual void OnDespawnServer(DespawnInfo info)
		{
			pipeData.OnDisable();
		}

		public virtual bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
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
					var Item = Spawn.ServerPrefab(SpawnOnDeconstruct, registerTile.WorldPositionServer, localRotation : this.transform.localRotation);
					Item.GameObject.GetComponent<PipeItem>().SetColour(Colour);
					OnDisassembly(interaction);
					pipeData.OnDisable();
					Despawn.ServerSingle(this.gameObject);
					return;
				}
			}

			Interaction(interaction);
		}

		public virtual void Interaction(HandApply interaction)
		{

		}

		public virtual void OnDisassembly(HandApply interaction)
		{

		}

		#region Editor

		void OnDrawGizmos()
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
