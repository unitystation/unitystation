using UnityEngine;
using US13.Core.Chat;
using US13.Core.Lifecycle;
using US13.Core.Transform;
using US13.Managers;
using US13.Managers.MatrixManager;
using US13.Objects.Directionals;
using US13.ScriptableObjects;
using US13.Systems.Spells;
using US13.Tilemaps.Tiles;
using US13.Tilemaps.Utils;
using Util;

namespace Mime
{
	public class MimeWallBig : Spell
	{
		[Tooltip("The obstruction object to spawn.")] [SerializeField]
		private LayerTile obstructionTile = default;

		[Tooltip("How long these obstructions last before disappearing.")] [SerializeField, Range(1, 600)]
		private int lifespan = 50;


		protected override string FormatInvocationMessage(PlayerInfo caster, string modPrefix)
		{
			return string.Format(SpellData.InvocationMessage, caster.Name,
				caster.Mind.CurrentCharacterSettings.ThemPronoun(caster.Script));
		}

		public override bool ValidateCast(PlayerInfo caster)
		{
			if (!base.ValidateCast(caster))
			{
				return false;
			}

			if (!caster.Mind.IsMiming)
			{
				Chat.AddExamineMsg(caster.GameObject, "You must dedicate yourself to silence first!");
				return false;
			}

			return true;
		}

		public override bool CastSpellServer(PlayerInfo caster)
		{
			Vector3Int[] obstructions = new Vector3Int[3];

			var Matrix = MatrixManager.AtPoint(caster.Script.WorldPos, true);

			if (caster.GameObject.TryGetComponent<Rotatable>(out var directional))
			{
				if (directional.CurrentDirection == OrientationEnum.Down_By180 ||
				    directional.CurrentDirection == OrientationEnum.Up_By0)
				{
					var Local = (directional.WorldDirection.To3() + caster.Script.WorldPos + Vector3.left)
						.ToLocal(Matrix).RoundToInt();
					var Local2 = (directional.WorldDirection.To3() + caster.Script.WorldPos + Vector3.right)
						.ToLocal(Matrix).RoundToInt();
					var Local3 = (directional.WorldDirection.To3() + caster.Script.WorldPos).ToLocal(Matrix)
						.RoundToInt();
					if (Matrix.MetaTileMap.HasTile(Local, LayerType.Walls) == false)
					{
						obstructions[0] = Local;
						Matrix.MetaTileMap.SetTile(Local, obstructionTile);
					}

					if (Matrix.MetaTileMap.HasTile(Local2, LayerType.Walls) == false)
					{
						obstructions[1] = Local;
						Matrix.MetaTileMap.SetTile(Local2, obstructionTile);
					}

					if (Matrix.MetaTileMap.HasTile(Local3, LayerType.Walls) == false)
					{
						obstructions[2] = Local3;
						Matrix.MetaTileMap.SetTile(Local3, obstructionTile);
					}
				}
				else if (directional.CurrentDirection == OrientationEnum.Left_By90 ||
				         directional.CurrentDirection == OrientationEnum.Right_By270)
				{
					var Local = (directional.WorldDirection.To3() + caster.Script.WorldPos + Vector3.up).ToLocal(Matrix)
						.RoundToInt();
					var Local2 = (directional.WorldDirection.To3() + caster.Script.WorldPos + Vector3.down)
						.ToLocal(Matrix).RoundToInt();
					var Local3 = (directional.WorldDirection.To3() + caster.Script.WorldPos).ToLocal(Matrix)
						.RoundToInt();
					if (Matrix.MetaTileMap.HasTile(Local, LayerType.Walls) == false)
					{
						obstructions[0] = Local;
						Matrix.MetaTileMap.SetTile(Local, obstructionTile);
					}

					if (Matrix.MetaTileMap.HasTile(Local2, LayerType.Walls) == false)
					{
						obstructions[1] = Local;
						Matrix.MetaTileMap.SetTile(Local2, obstructionTile);
					}

					if (Matrix.MetaTileMap.HasTile(Local3, LayerType.Walls) == false)
					{
						obstructions[2] = Local3;
						Matrix.MetaTileMap.SetTile(Local3, obstructionTile);
					}
				}
			}

			StartCoroutine(DespawnObstructions(obstructions, Matrix));

			return true;
		}

		private System.Collections.IEnumerator DespawnObstructions(Vector3Int[] Positions, MatrixInfo MatrixInfo)
		{
			yield return WaitFor.Seconds(lifespan);

			foreach (var Position in Positions)
			{
				MatrixInfo.TileChangeManager.MetaTileMap.RemoveTileWithlayer(Position, obstructionTile.LayerType);
			}
		}
	}
}