using PlayGroups.Input;
using Tilemaps.Scripts.Behaviours.Interaction;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours
{
	public class TileTrigger : InputTrigger
	{
		private Layer layer;

		private void Start()
		{
			layer = GetComponent<Layer>();
		}

		public override void Interact(GameObject originator, Vector3 position, string hand)
		{
			Vector3Int pos = Vector3Int.RoundToInt(transform.InverseTransformPoint(position));
			pos.z = 0;

			LayerTile tile = layer.GetTile(pos);

			if (tile?.TileType == TileType.Table)
			{
				TableInteraction interaction = new TableInteraction(gameObject, originator, position, hand);

				interaction.Interact(isServer);
			}
		}
	}
}