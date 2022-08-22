using System.Collections;
using System.Collections.Generic;
using Shared.Managers;
using UnityEngine;

namespace Core.Lighting
{
	public class HighlightScanManager : SingletonManager<HighlightScanManager>
	{
		public HashSet<HighlightScan> HighlightScans = new HashSet<HighlightScan>();

		public float MaximumDistanceBetweenPlayerAndScanObjects = 50f;
		public int MaximumHighlightCallsPerFrame = 25;

		public override void Start()
		{
			base.Start();
			if (CustomNetworkManager.IsHeadless) Destroy(this);
		}

		public void Highlight()
		{
			StartCoroutine(HighlightOnceEveryFrame());
		}

		IEnumerator HighlightOnceEveryFrame()
		{
			var totalScanned = 0;
			HighlightScans.Remove(null);
			if (PlayerManager.LocalPlayerObject == null || PlayerManager.LocalPlayerScript.IsDeadOrGhost) yield break;
			foreach (var scan in HighlightScans)
			{
				totalScanned++;
				if (totalScanned > MaximumHighlightCallsPerFrame)
				{
					yield return WaitFor.EndOfFrame;
					totalScanned = 0;
				}
				if(Vector3.Distance(PlayerManager.LocalPlayerObject.transform.position, scan.gameObject.transform.position) > MaximumDistanceBetweenPlayerAndScanObjects ) continue;
				StartCoroutine(scan.Highlight());
			}
		}
	}
}