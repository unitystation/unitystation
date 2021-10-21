using System.Collections.Generic;
using UnityEngine;
using Light2D;
using HealthV2;
using Systems.Pipes;
using Items;


namespace Systems.Explosions
{
	public class ExplosionNode
	{
		public Vector2Int Location;
		public Matrix matrix;

		public HashSet<ExplosionPropagationLine> PresentLines = new HashSet<ExplosionPropagationLine>();
		public Vector2 AngleAndIntensity;

		public List<PipeNode> SavedPipes = new List<PipeNode>();

		public void Initialise(Vector2Int Loc, Matrix Inmatrix)
		{
			Location = Loc;
			matrix = Inmatrix;
		}

		public void Process()
		{
			float Damagedealt = AngleAndIntensity.magnitude;
			float EnergyExpended = 0;
			var v3int = new Vector3Int(Location.x, Location.y, 0);

			var metaTileMap = matrix.MetaTileMap;

			if (Damagedealt <= 0)
			{
				return;
			}

			if (metaTileMap == null)
			{
				return;
			}

			EnergyExpended = metaTileMap.ApplyDamage(v3int, Damagedealt,
			MatrixManager.LocalToWorldInt(v3int, matrix.MatrixInfo), AttackType.Bomb);

			if (Damagedealt > 100)
			{
				var Node = matrix.GetMetaDataNode(v3int);
				if (Node != null)
				{
					foreach (var electricalData in Node.ElectricalData)
					{
						electricalData.InData.DestroyThisPlease();
					}

					SavedPipes.Clear();
					SavedPipes.AddRange(Node.PipeData);
					foreach (var Pipe in SavedPipes)
					{
						Pipe.pipeData.DestroyThis();
					}
				}
			}


			foreach (var integrity in matrix.Get<Integrity>(v3int, true))
			{
				//Throw items
				if(integrity.GetComponent<ItemAttributesV2>() != null)
				{
					ThrowInfo throwInfo = new ThrowInfo
					{
						//the thrown object is itself for now, in case ThrownBy breaks if null
						ThrownBy = integrity.gameObject,
						Aim = BodyPartType.Chest,
						OriginWorldPos = integrity.RegisterTile.WorldPosition,
						WorldTrajectory = AngleAndIntensity.Rotate90(),
						SpinMode = RandomUtils.RandomSpin()
					};

					integrity.GetComponent<CustomNetTransform>().Throw(throwInfo);
				}

				//And do damage to objects
				integrity.ApplyDamage(Damagedealt, AttackType.Bomb, DamageType.Brute);
			}

			foreach (var player in matrix.Get<ObjectBehaviour>(v3int, ObjectType.Player, true))
			{

				// do damage
				player.GetComponent<PlayerHealthV2>().ApplyDamageAll(null, Damagedealt, AttackType.Bomb, DamageType.Brute);

			}

			foreach (var line in PresentLines)
			{
				line.ExplosionStrength -= EnergyExpended * (line.ExplosionStrength / Damagedealt);
			}
			AngleAndIntensity = Vector2.zero;

		}
	}
}
