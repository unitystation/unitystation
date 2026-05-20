using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.Managers;
using US13.Managers.UpdateManager;
using US13.Systems.Explosions.NodeTypes;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Utils;

namespace US13.Systems.Explosions
{
	public class ExplosionManager : MonoBehaviour
	{
		public static HashSet<ExplosionNode> CheckLocations = new HashSet<ExplosionNode>();
		public static HashSet<ExplosionPropagationLine> CheckLines = new HashSet<ExplosionPropagationLine>();
		private static HashSet<ExplosionPropagationLine> SubCheckLines = new HashSet<ExplosionPropagationLine>();

		public static List<EffectDataToClean> DelayedEffectsToRemove = new List<EffectDataToClean>();

		public const float DEFAULT_EXPLOSION_STEP_TIME_IN_SECONDS = 0.4f;


		public class EffectDataToClean
		{
			public static Stack<EffectDataToClean> PooledEffectDataToClean = new Stack<EffectDataToClean>();

			public float TimeLeft;
			public OverlayType effectOverlayType;
			public Vector3Int position;
			public MetaTileMap MetaTileMap;
			public GameObject Firelight;

			public struct EffectCallback
			{
				public float explosionStrength;
				public Vector3 position;
				public OnCleanEffect callBackAction;
			}
			public delegate void OnCleanEffect(Vector3 position, float strength);

			public EffectCallback Callback;

			public void Pool()
			{
				PooledEffectDataToClean.Push(this);
			}

			public static EffectDataToClean Get()
			{
				if (PooledEffectDataToClean.Count > 0)
				{
					return PooledEffectDataToClean.Pop();
				}
				else
				{
					return new EffectDataToClean();
				}
			}
		}

		public static void CleanupEffectLater(float seconds, MetaTileMap MetaTileMap, Vector3Int position, OverlayType effectOverlayType, GameObject Firelight, EffectDataToClean.EffectCallback callback = new EffectDataToClean.EffectCallback())
		{
			var EffectData = EffectDataToClean.Get();
			EffectData.TimeLeft = Mathf.Min((int) seconds, 5);
			EffectData.MetaTileMap = MetaTileMap;
			EffectData.position = position;
			EffectData.effectOverlayType = effectOverlayType;
			EffectData.Firelight = Firelight;
			EffectData.Callback = callback;
			DelayedEffectsToRemove.Add(EffectData);
		}


		private void OnEnable()
		{
			if (Application.isEditor == false && NetworkServer.active == false) return;

			UpdateManager.Add(Step, GameConfigManager.GameConfig.ExplosionStepTimeInSeconds > 0 ?
				GameConfigManager.GameConfig.ExplosionStepTimeInSeconds : DEFAULT_EXPLOSION_STEP_TIME_IN_SECONDS);
		}

		private void OnDisable()
		{
			if(Application.isEditor == false && NetworkServer.active == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Step);
		}

		public void Step()
		{
			SubCheckLines.UnionWith(CheckLines);
			CheckLines.Clear();
			foreach (var checkLine in SubCheckLines)
			{
				checkLine.Step();
			}
			SubCheckLines.Clear();

			foreach (ExplosionNode explosionNode in CheckLocations.ToArray())
			{
				CheckLocations.Remove(explosionNode); //lets not create infinite explosions in the case of a runtime
				_ = explosionNode.Process();
			}

			for (int i = DelayedEffectsToRemove.Count - 1; i >= 0; i--)
			{
				var timeEffect = DelayedEffectsToRemove[i];
				timeEffect.TimeLeft = timeEffect.TimeLeft -0.4f; //Not the most accurate but good enough
				if (timeEffect.TimeLeft < 0)
				{
					timeEffect.MetaTileMap.RemoveOverlaysOfType(timeEffect.position, LayerType.Effects, timeEffect.effectOverlayType);
					if (timeEffect.Firelight != null)
					{
						_ = Despawn.ServerSingle(timeEffect.Firelight);
					}

					DelayedEffectsToRemove.RemoveAt(i);
					timeEffect.Callback.callBackAction?.Invoke(timeEffect.Callback.position, timeEffect.Callback.explosionStrength);
					timeEffect.Pool();
				}
			}
		}
	}
}