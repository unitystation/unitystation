using System;
using System.Runtime.Remoting;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    [ExecuteInEditMode]
    public class RegisterObject : RegisterTile
    {
        [HideInInspector]
        public Vector3Int Offset = Vector3Int.zero;

        public bool Passable = true;
        public bool AtmosPassable = true;
        
        protected override void OnAddTile(Vector3Int oldPosition, Vector3Int newPosition)
        {
            var obj = layer?.Objects.GetFirst<RegisterObject>(newPosition);

            if (obj != null)
            {
                DestroyImmediate(obj.gameObject);
            }
            
            base.OnAddTile(oldPosition, newPosition);
        }

        public override bool IsPassable()
        {
            return Passable;
        }

        public override bool IsAtmosPassable()
        {
            return AtmosPassable;
        }
    }
}