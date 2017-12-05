using Tilemaps.Scripts.Behaviours.Layers;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    [ExecuteInEditMode]
    public class RegisterPlayer : RegisterTile
    {
        public bool IsBlocking { get; set; } = true;

        public override bool IsPassable()
        {
            return !IsBlocking;
        }
    }
}
