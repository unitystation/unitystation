using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Light2D;
using HealthV2;
using Systems.Pipes;
using Items;
using TileManagement;
using AddressableReferences;
using Core;
using Core.Lighting_System.Light2D;
using Logs;
using Player;
using Scripts.Core.Transform;
using Systems.Atmospherics;
using UniversalObjectPhysics = Core.Physics.UniversalObjectPhysics;


namespace Systems.Explosions
{
	public class ExplosionNode
	{
		public Vector3Int Location;
		public Matrix matrix;

		public HashSet<ExplosionPropagationLine> PresentLines = new HashSet<ExplosionPropagationLine>();
		public Vector2 AngleAndIntensity;

		public List<PipeNode> SavedPipes = new List<PipeNode>();

		public List<ItemTrait> IgnoreAttributes = new List<ItemTrait>();

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

		//method that, surprise, does damage to stuff on node's tile. override for custom behaviour. must return EnergyExpended value
		public virtual float DoDamage(Matrix matrix, float damageDealt, Vector3Int v3int)
		{
			var metaTileMap = matrix.MetaTileMap;
			float energyExpended = metaTileMap.ApplyDamage(v3int, damageDealt,
			MatrixManager.LocalToWorldInt(v3int, matrix.MatrixInfo), AttackType.Bomb);

			DamageLayers(damageDealt, v3int);

			foreach (var integrity in matrix.Get<Integrity>(v3int, true))
			{
				//Throw items and Objects
				integrity.GetComponent<UniversalObjectPhysics>()?.NewtonianNewtonPush(AngleAndIntensity.Rotate90(), AngleAndIntensity.magnitude * 0.1f , 1, 3,
					BodyPartType.Chest, integrity.gameObject, 15);

				if (integrity.TryGetComponent<ItemAttributesV2>(out var traits))
				{
					if (IgnoreAttributes != null && traits.HasAnyTrait(IgnoreAttributes)) continue;
				}

				//And do damage to objects
				integrity.ApplyDamage(damageDealt, AttackType.Bomb, DamageType.Brute);
			}

			foreach (var player in matrix.Get<LivingHealthMasterBase>(v3int, ObjectType.Player, true))
			{
				player.GetComponent<UniversalObjectPhysics>()?.NewtonianPush(AngleAndIntensity.Rotate90(), 7, 1, 3,
                					BodyPartType.Chest, player.gameObject, 15);
				// do damage
				player.ApplyDamageAll(null, damageDealt, AttackType.Bomb, DamageType.Brute, default, TraumaticDamageTypes.NONE, 75);
			}

			ChangeNodeTemp(matrix, damageDealt, v3int);

			return energyExpended;
		}

		private void ChangeNodeTemp(Matrix matrix, float damageDealt, Vector3Int v3int)
		{
			try
			{
				if (matrix.ReactionManager != null)
				{
					matrix.ReactionManager.ExposeHotspot(v3int, 350 * damageDealt, true);
				}
			}
			catch (Exception e)
			{
				Loggy.Log("[ExplosionNode/DoDamage] - Something went wrong while trying to change tile temperature:\n "+ e.ToString());
			}
		}

		protected void DamageLayers(float damageDealt, Vector3Int v3int)
		{
			if (damageDealt < 100) return;
			var node = matrix.GetMetaDataNode(v3int);
			if (node == null) return;
			foreach (var electricalData in node.ElectricalData)
			{
				electricalData.InData.DestroyThisPlease();
			}
			if (damageDealt > 135)
			{
				foreach (var disposalPipe in node.DisposalPipeData)
				{
					matrix.TileChangeManager.MetaTileMap.RemoveTileWithlayer(disposalPipe.NodeLocation, LayerType.Disposals);
				}
			}
			if (damageDealt > 200)
			{
				SavedPipes.Clear();
				SavedPipes.AddRange(node.PipeData);
				foreach (var Pipe in SavedPipes)
				{
					Pipe.pipeData.Remove();
				}
			}
		}

		//triggered by ChemExplosion, this method says what to do when explosion is inside body
		public virtual void DoInternalDamage(float strength, BodyPart bodyPart)
		{
			if (strength >= bodyPart.Health)
			{
				float temp = bodyPart.Health; //temporary store to make sure we don't use an updated health when decrementing strength
				bodyPart.TakeDamage(null, temp, AttackType.Internal, DamageType.Brute,
					default, default, default, 0);
				strength -= temp;
			}
			else
			{
				bodyPart.TakeDamage(null, strength, AttackType.Internal, DamageType.Brute,
					default, default, default, 0);
				strength = 0;
			}

			foreach (BodyPart part in bodyPart.HealthMaster.BodyPartList)
			{
				if (strength >= part.Health)
				{
					float temp = part.Health; //temporary store to make sure we don't use an updated health when decrementing strength
					bodyPart.TakeDamage(null, temp, AttackType.Internal, DamageType.Brute,
						default, default, default, 0);
					strength -= temp;
				}
				else
				{
					bodyPart.TakeDamage(null, strength, AttackType.Internal, DamageType.Brute,
						default, default, default, 0);
					strength = 0;
				}
			}
		}

		public void TimedEffect(Vector3Int position, float time, string effectName, OverlayType effectOverlayType, TileChangeManager tileChangeManager)
		{
			//Dont add effect if it is already there
			if (tileChangeManager.MetaTileMap.HasOverlay(position, TileType.Effects, effectName)) return;
			tileChangeManager.MetaTileMap.AddOverlay(position, TileType.Effects, effectName);
			var Position = position.ToWorld(tileChangeManager.MetaTileMap.matrix);
			var fireLightSpawn = Spawn.ServerPrefab(tileChangeManager.MetaTileMap.matrix.ReactionManager.FireLightPrefab,Position );

			fireLightSpawn.GameObject.GetComponent<UniversalObjectPhysics>().AppearAtWorldPositionServer(Position);
			fireLightSpawn.GameObject.GetComponent< ScaleSync>().SetScale(Vector3.one * 30);
			ExplosionManager.CleanupEffectLater(time * 0.001f, tileChangeManager.MetaTileMap,
				position, effectOverlayType, fireLightSpawn.GameObject);
		}

		public virtual ExplosionNode GenInstance()
		{
			return new ExplosionNode();
		}
	}
}
