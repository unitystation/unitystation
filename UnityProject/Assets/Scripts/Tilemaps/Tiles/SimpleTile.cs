using System;
using System.Collections.Generic;
using Core.Lighting;
using NaughtyAttributes;
using UnityEngine;

namespace Tiles
{
	public class SimpleTile : BasicTile
	{
		public Sprite sprite;

		public override Sprite PreviewSprite => sprite;

		public bool CanBeHighlightedThroughScanners = false;

		public event Action OnDestroyed;
		[HideInInspector] public List<GameObject> AssoicatedSpawnedObjects;

		[ShowIf(nameof(CanBeHighlightedThroughScanners))]
		public GameObject HighlightObject;


		private void OnDestroy()
		{
			foreach (var obj in AssoicatedSpawnedObjects)
			{
				Despawn.ClientSingle(obj);
			}
		}
	}
}
