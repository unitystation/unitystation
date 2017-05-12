using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class World
{
	public static List<Collider2D> GetGameObjectsWithinTile(Vector2 position, LayerMask layerMask)
	{
		var snappedPosition = new Vector2 (Mathf.Round (position.x), Mathf.Round (position.y));

		var objects = Physics2D.OverlapBoxAll(snappedPosition, new Vector2(0.9f, 0.9f), 0, 1 << layerMask).ToList();

		return objects;
	}

	public static void ReorderGameobjectsOnTile(Vector2 position) {
		//get number of objects already at this position order them and then make the new item the highest
		List<Collider2D> colliders = World.GetGameObjectsWithinTile(position, LayerMask.NameToLayer("Items"));
		colliders.Reverse ();
		int orderCount = 1;
		for (int i = 0; i < colliders.Count; i++) {
			var collider = colliders [i];
			var sRenderer = collider.gameObject.GetComponentInChildren<SpriteRenderer> ();
			if (sRenderer != null) {
				sRenderer.sortingOrder = orderCount;
			}
			orderCount++;
		}
	}
}

