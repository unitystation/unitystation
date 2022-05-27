using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Light2D;
using HealthV2;
using Systems.Pipes;
using Items;
using Items.Others;
using Systems.Electricity;
using TileManagement;
using AddressableReferences;


namespace Systems.Explosions
{
	public class ExplosionNode
	{
		public Vector3Int Location;
		public Matrix matrix;

		public HashSet<ExplosionPropagationLine> PresentLines = new HashSet<ExplosionPropagationLine>();
		public Vector2 AngleAndIntensity;

		public List<PipeNode> SavedPipes = new List<PipeNode>();

		public virtual string EffectName
		{
			get { return "Fire"; }
		}
		public virtual OverlayType EffectOverlayType
		{
			get { return OverlayType.Fire; }
		}
		public virtual AddressableAudioSource CustomSound
		{
			get { return null; }
		}

		public void Initialise(Vector3Int Loc, Matrix Inmatrix)
		{
			Location = Loc;
			matrix = Inmatrix;
		}

		public void Process()
		{
			float DamageDealt = AngleAndIntensity.magnitude;
			float EnergyExpended = 0;
			var v3int = new Vector3Int(Location.x, Location.y, 0);
			var tileManager = matrix.TileChangeManager;

			if (DamageDealt <= 0)
			{
				return;
			}

			if (matrix.MetaTileMap == null)
			{
				return;
			}

			if(DamageDealt > 0)
			{
				if (EffectName != null && EffectOverlayType != null && tileManager != null)
				{
					TimedEffect(v3int, DamageDealt * 10, EffectName, EffectOverlayType, tileManager);
				}
				EnergyExpended = DoDamage(matrix, DamageDealt, v3int);

				foreach (var line in PresentLines)
				{
					line.ExplosionStrength -= EnergyExpended * (line.ExplosionStrength / DamageDealt);
				}
				AngleAndIntensity = Vector2.zero;
			}
		}

		private void debugSquare(Vector3 pos)
		{
			Debug.DrawLine(new Vector2(pos.x - 0.5f, pos.y - 0.5f), new Vector2(pos.x + 0.5f, pos.y - 0.5f), Color.blue, 10);
			Debug.DrawLine(new Vector2(pos.x - 0.5f, pos.y + 0.5f), new Vector2(pos.x + 0.5f, pos.y + 0.5f), Color.blue, 10);
			Debug.DrawLine(new Vector2(pos.x - 0.5f, pos.y - 0.5f), new Vector2(pos.x - 0.5f, pos.y + 0.5f), Color.blue, 10);
			Debug.DrawLine(new Vector2(pos.x + 0.5f, pos.y - 0.5f), new Vector2(pos.x + 0.5f, pos.y + 0.5f), Color.blue, 10);
		}

		//method that, surprise, does damage to stuff on node's tile. override for custom behaviour. must return EnergyExpended value
		public virtual float DoDamage(Matrix matrix, float DamageDealt, Vector3Int v3int)
		{
			var metaTileMap = matrix.MetaTileMap;
			float EnergyExpended = metaTileMap.ApplyDamage(v3int, DamageDealt,
			MatrixManager.LocalToWorldInt(v3int, matrix.MatrixInfo), AttackType.Bomb);

			if (DamageDealt > 100)
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
				if (integrity.GetComponent<ItemAttributesV2>() != null)
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
				integrity.ApplyDamage(DamageDealt, AttackType.Bomb, DamageType.Brute);
			}

			foreach (var player in matrix.Get<ObjectBehaviour>(v3int, ObjectType.Player, true))
			{

				// do damage
				player.GetComponent<PlayerHealthV2>().ApplyDamageAll(null, DamageDealt, AttackType.Bomb, DamageType.Brute);

			}
			return EnergyExpended;
		}

		public async Task TimedEffect(Vector3Int position, float time, string effectName, OverlayType effectOverlayType, TileChangeManager tileChangeManager)
        {
			//Dont add effect if it is already there
			if (tileChangeManager.MetaTileMap.HasOverlay(position, TileType.Effects, effectName)) return;

			tileChangeManager.MetaTileMap.AddOverlay(position, TileType.Effects, effectName);
			await Task.Delay((int)time);
			tileChangeManager.MetaTileMap.RemoveOverlaysOfType(position, LayerType.Effects, effectOverlayType);
		}
	}
}
