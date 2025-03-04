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
using Cysharp.Threading.Tasks;
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

		public virtual async UniTask Process()
		{
			float damageDealt = AngleAndIntensity.magnitude;
			if (damageDealt <= 0)
			{
				return;
			}

			if (matrix.MetaTileMap == null)
			{
				return;
			}


			if (damageDealt > 0)
			{
				//(Max): This is a terrible name. Whoever named it this way should be ashamed.
				//I have no clue what's the context of this vector. Is it local position? Is it world position? Is it a direction? Who knows!
				//Keep gatekeeping the codebase, it's not like there are other people working on this project..
				var v3int = new Vector3Int(Location.x, Location.y, 0);
				await ReguralProcessingToTilesOnly(damageDealt, v3int);
				_ = DoDamageToObjects(matrix, damageDealt, v3int);
			}
		}

		protected async UniTask ReguralProcessingToTilesOnly(float damageDealt, Vector3Int v3int)
		{
			var tileManager = matrix.TileChangeManager;
			if (EffectName != null && EffectOverlayType != null && tileManager != null)
			{
				await TimedEffect(v3int, damageDealt * 10, EffectName, EffectOverlayType, tileManager);
			}
			var metaTileMap = matrix.MetaTileMap;
			var energyExpended = DoDamageToTiles(matrix, damageDealt, v3int, metaTileMap);
			foreach (var line in PresentLines)
			{
				line.ExplosionStrength -= energyExpended * (line.ExplosionStrength / damageDealt);
			}
			AngleAndIntensity = Vector2.zero;
		}

		//method that, surprise, does damage to stuff on node's tile. override for custom behaviour. must return EnergyExpended value
		public virtual float DoDamageToTiles(Matrix matrix, float damageDealt, Vector3Int v3int, MetaTileMap metaTileMap)
		{
			float energyExpended = metaTileMap.ApplyDamage(v3int, damageDealt,
			MatrixManager.LocalToWorldInt(v3int, matrix.MatrixInfo), AttackType.Bomb);

			DamageLayers(damageDealt, v3int);
			ChangeNodeTemp(matrix, damageDealt, v3int);

			return energyExpended;
		}

		public virtual async UniTask DoDamageToObjects(Matrix matrix, float damageDealt, Vector3Int v3int)
		{
			foreach (var integrity in matrix.Get<Integrity>(v3int, true))
			{
				// Incase of multiple explosions occuring at once (i.e: multiple Gibtonites)
				// trycatch this to avoid trying to destroy stuff that is already destroyed by other damage sources.
				try
				{
					integrity.Physics.NewtonianNewtonPush(AngleAndIntensity.Rotate90(), AngleAndIntensity.magnitude * 0.1f , 1, 3,
						BodyPartType.Chest, integrity.gameObject, 15);

					if (IgnoreAttributes != null)
					{
						await UniTask.WaitForEndOfFrame();
						if (integrity.TryGetComponent<ItemAttributesV2>(out var traits) &&
						    traits.HasAnyTraitZeroAlloc(IgnoreAttributes)) continue;
					}

					// And do damage to objects
					integrity.ApplyDamage(damageDealt, AttackType.Bomb, DamageType.Brute);
				}
				catch (Exception e)
				{
					Loggy.Error($"An error occured while trying to damage an object. Maybe it's no longer avaliable?\n {e}");
				}
			}

			// Damage mobs
			foreach (var player in matrix.Get<LivingHealthMasterBase>(v3int, ObjectType.Player, true))
			{
				//Player damage is relatively fine performance wise, and doesn't generate any GC.
				//However, it is still a bit too complex; and it's complexity shows when several mobs are recieving damage at once.
				//Processing one at a time each frame should give the game plenty of breathing room.
				await UniTask.WaitForEndOfFrame();
				try
				{
					player.playerScript.playerMove.NewtonianPush(AngleAndIntensity.Rotate90(), 7, 1, 3,
						BodyPartType.Chest, player.gameObject, 15);
					player.ApplyDamageAll(null, damageDealt, AttackType.Bomb, DamageType.Brute, default, TraumaticDamageTypes.NONE, 75);
				}
				catch (Exception e)
				{
					Loggy.Error(
						$"An issue occured while trying to damage players during an explosion. Maybe they got gibbed?\n {e}");
				}
			}
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
				Loggy.Error("[ExplosionNode/DoDamage] - Something went wrong while trying to change tile temperature:\n "+ e.ToString());
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

		public UniTask TimedEffect(Vector3Int position, float time, string effectName, OverlayType effectOverlayType, TileChangeManager tileChangeManager)
		{
			//Dont add effect if it is already there
			if (tileChangeManager.MetaTileMap.HasOverlay(position, TileType.Effects, effectName)) return UniTask.CompletedTask;
			tileChangeManager.MetaTileMap.AddOverlay(position, TileType.Effects, effectName);
			var Position = position.ToWorld(tileChangeManager.MetaTileMap.matrix);
			//TODO: Pool this because it's ruining performance heavily when multiple explosions occur.
			var fireLightSpawn = Spawn.ServerPrefab(tileChangeManager.MetaTileMap.matrix.ReactionManager.FireLightPrefab, Position);
			var physics =
				ComponentsTracker<UniversalObjectPhysics>.GetComponentFromGameObject(fireLightSpawn.GameObject);
			if (physics != null)
			{
				physics.AppearAtWorldPositionServer(Position);
				physics.Scale.SetScale(Vector3.one * 30);
			}
			ExplosionManager.CleanupEffectLater(time * 0.001f, tileChangeManager.MetaTileMap,
				position, effectOverlayType, fireLightSpawn.GameObject);
			return UniTask.CompletedTask;
		}

		public virtual ExplosionNode GenInstance()
		{
			return new ExplosionNode();
		}
	}
}
