using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace Systems.Explosions
{
	public class ExplosionManager : MonoBehaviour
	{
		public static HashSet<ExplosionNode> CheckLocations = new HashSet<ExplosionNode>();
		public static HashSet<ExplosionPropagationLine> CheckLines = new HashSet<ExplosionPropagationLine>();
		private static HashSet<ExplosionPropagationLine> SubCheckLines = new HashSet<ExplosionPropagationLine>();

		private void OnEnable()
		{
			if(Application.isEditor == false && NetworkServer.active == false) return;

			UpdateManager.Add(Step, 0.25f);
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

			foreach (var explosionNode in CheckLocations.ToArray())
			{
				CheckLocations.Remove(explosionNode); //lets not create infinite explosions in the case of a runtime
				explosionNode.Process();
			}
		}
	}
}