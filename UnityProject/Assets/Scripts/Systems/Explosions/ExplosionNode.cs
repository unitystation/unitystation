using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;

namespace Explosions
{
	public class ExplosionNode
	{
		public Vector2Int Location;
		public Matrix matrix;


		public HashSet<ExplosionPropagationLine> PresentLines = new HashSet<ExplosionPropagationLine>();
		public Vector2 AngleAndIntensity;

		public List<Pipes.PipeNode> SavedPipes = new List<Pipes.PipeNode>();





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

			if (metaTileMap == null)
			{
				return;
			}

			EnergyExpended = metaTileMap.ApplyDamage(v3int, Damagedealt,
			MatrixManager.LocalToWorldInt(v3int, matrix.MatrixInfo), AttackType.Bomb) * 0.375f;

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
				//And do damage to objects
				integrity.ApplyDamage(Damagedealt, AttackType.Bomb, DamageType.Brute);
			}

			foreach (var player in matrix.Get<ObjectBehaviour>(v3int, ObjectType.Player, true))
			{

				// do damage
				player.GetComponent<PlayerHealth>().ApplyDamage(null, Damagedealt, AttackType.Bomb, DamageType.Brute);

			}

			foreach (var line in PresentLines)
			{
				line.ExplosionStrength -= EnergyExpended * (line.ExplosionStrength / Damagedealt);
			}
			AngleAndIntensity = Vector2.zero;

		}
	}
}

