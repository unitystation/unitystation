using System;
using System.Collections;
using System.Collections.Generic;
using Shared.Managers;
using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.Player;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Tiles;
using Util;

namespace US13.Core.Lighting
{
	public class HighlightScanManager : SingletonManager<HighlightScanManager>
	{
		public HashSet<HighlightScan> HighlightScans = new HashSet<HighlightScan>();
		public List<UninitialisedHighlightScan> UninitialisedHighlightScans = new List<UninitialisedHighlightScan>();


		public static void RemoveHighlight(Vector3Int LocalPOS,SimpleTile Tile, Layer Layer)
		{
			if (CustomNetworkManager.IsHeadless) return;
			Instance.HighlightScans.RemoveWhere(x=> x.LocalPOS == LocalPOS && x.Tile == Tile  && x.Layer == Layer );
			Instance.UninitialisedHighlightScans.RemoveAll(x=> x.LocalPOS == LocalPOS && x.Tile == Tile  && x.Layer == Layer );
		}


		public struct UninitialisedHighlightScan : IEquatable<UninitialisedHighlightScan>
		{
			public Vector3Int LocalPOS;
			public Layer Layer;
			public SimpleTile Tile;

			public bool Equals(UninitialisedHighlightScan other)
			{
				return LocalPOS.Equals(other.LocalPOS) && Equals(Layer, other.Layer) && Equals(Tile, other.Tile);
			}

			public override bool Equals(object obj)
			{
				return obj is UninitialisedHighlightScan other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(LocalPOS, Layer, Tile);
			}
		}

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
					yield return    WaitFor.EndOfFrame;
					totalScanned = 0;
				}
				if(Vector3.Distance(PlayerManager.LocalPlayerObject.transform.position, scan.gameObject.transform.position) > MaximumDistanceBetweenPlayerAndScanObjects ) continue;
				StartCoroutine(scan.Highlight());
			}

			for (int i = UninitialisedHighlightScans.Count - 1; i >= 0; i--)
			{
				var scan = UninitialisedHighlightScans[i];
				totalScanned++;
				if (totalScanned > MaximumHighlightCallsPerFrame)
				{
					yield return WaitFor.EndOfFrame;
					totalScanned = 0;
				}
				if(Vector3.Distance(PlayerManager.LocalPlayerObject.transform.position, scan.LocalPOS.ToWorld(scan.Layer.Matrix)) > MaximumDistanceBetweenPlayerAndScanObjects ) continue;
				scan.Layer.AddHighlight(scan);
				UninitialisedHighlightScans.Remove(scan);
			}
		}
	}
}