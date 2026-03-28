using System;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using US13.Core.Lifecycle;

namespace US13.Tilemaps.Tiles
{
	public class SimpleTile : BasicTile
	{


		public bool CanBeHighlightedThroughScanners = false;



		[ShowIf(nameof(CanBeHighlightedThroughScanners))]
		public GameObject HighlightObject;

	}
}
