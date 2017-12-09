using System;
using InputControl;
using PlayGroup;
using PlayGroups.Input;
using Tilemaps.Scripts.Behaviours.Interaction;
using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Tiles;
using UI;
using UnityEngine;
using UnityEngine.Networking;

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
            var pos = Vector3Int.RoundToInt(transform.InverseTransformPoint(position));
            pos.z = 0;

            var tile = layer.GetTile(pos);

            if (tile?.TileType == TileType.Table)
            {
                var interaction = new TableInteraction(gameObject, originator, position, hand);
                
                interaction.Interact(isServer);
            }
        }
    }
}