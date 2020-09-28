using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Explosions
{
	public class ExplosionManager : MonoBehaviour
	{
		public static HashSet<ExplosionNode> CheckLocations = new HashSet<ExplosionNode>();
		public static HashSet<ExplosionPropagationLine> CheckLines = new HashSet<ExplosionPropagationLine>();
		private static HashSet<ExplosionPropagationLine> SubCheckLines = new HashSet<ExplosionPropagationLine>();

		private void OnEnable()
		{
			UpdateManager.Add(Step, 0.25f);
		}

		private void OnDisable()
		{
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

			foreach (var CheckLoc in CheckLocations)
			{
				CheckLoc.Process();
			}
			CheckLocations.Clear();
		}
	}


}
