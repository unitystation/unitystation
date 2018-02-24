﻿using PlayGroups.Input;
using Tilemaps.Behaviours.Interaction;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Tiles;
using UnityEngine;
using Util;

namespace Tilemaps.Behaviours
{
	public class TileTrigger : InputTrigger
	{
		private MetaTileMap metaTileMap;
		private ObjectLayer objectLayer;

		private void Start()
		{
			metaTileMap = GetComponent<MetaTileMap>();
			objectLayer = GetComponentInChildren<ObjectLayer>();
		}

		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			Vector3Int pos = objectLayer.transform.InverseTransformPoint(position).RoundToInt();
			pos.z = 0;

			LayerTile tile = metaTileMap.GetTile(pos);

			if (tile?.TileType == TileType.Table)
			{
				TableInteraction interaction = new TableInteraction(gameObject, originator, position, hand);

				interaction.Interact(isServer);
			}
		}
	}
}