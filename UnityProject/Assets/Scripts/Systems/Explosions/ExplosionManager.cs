using System.Collections.Generic;
using System.Linq;
using Mirror;
using TileManagement;
using UnityEngine;

namespace Systems.Explosions
{
	public class ExplosionManager : MonoBehaviour
	{
		public static HashSet<ExplosionNode> CheckLocations = new HashSet<ExplosionNode>();
		public static HashSet<ExplosionPropagationLine> CheckLines = new HashSet<ExplosionPropagationLine>();
		private static HashSet<ExplosionPropagationLine> SubCheckLines = new HashSet<ExplosionPropagationLine>();


		public static List<EffectDataToClean> DelayedEffectsToRemove = new List<EffectDataToClean>();


		public class EffectDataToClean
		{
			public static Stack<EffectDataToClean> PooledEffectDataToClean = new Stack<EffectDataToClean>();

			public float TimeLeft;
			public OverlayType effectOverlayType;
			public Vector3Int position;
			public MetaTileMap MetaTileMap;

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

		public static void CleanupEffectLater(float seconds, MetaTileMap MetaTileMap, Vector3Int position, OverlayType effectOverlayType)
		{
			var EffectData = EffectDataToClean.Get();
			EffectData.TimeLeft = Mathf.Min((int) seconds, 5);
			EffectData.MetaTileMap = MetaTileMap;
			EffectData.position = position;
			EffectData.effectOverlayType = effectOverlayType;
			DelayedEffectsToRemove.Add(EffectData);
		}


		private void OnEnable()
		{
			if(Application.isEditor == false && NetworkServer.active == false) return;

			UpdateManager.Add(Step, 0.5f);
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
			foreach (var CheckLine in SubCheckLines)
			{
				CheckLine.Step();
			}
			SubCheckLines.Clear();

			foreach (ExplosionNode explosionNode in CheckLocations.ToArray())
			{
				CheckLocations.Remove(explosionNode); //lets not create infinite explosions in the case of a runtime
				explosionNode.Process();
			}

			for (int i = DelayedEffectsToRemove.Count - 1; i >= 0; i--)
			{
				var timeEffect = DelayedEffectsToRemove[i];
				timeEffect.TimeLeft = timeEffect.TimeLeft -0.5f; //Not the most accurate but good enough
				if (timeEffect.TimeLeft < 0)
				{
					timeEffect.MetaTileMap.RemoveOverlaysOfType(timeEffect.position, LayerType.Effects, timeEffect.effectOverlayType);
					DelayedEffectsToRemove.RemoveAt(i);
					timeEffect.Pool();
				}
			}
		}
	}
}