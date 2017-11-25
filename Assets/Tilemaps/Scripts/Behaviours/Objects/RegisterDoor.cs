using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    [ExecuteInEditMode]
    public class RegisterDoor : RegisterObject
    {
        public bool closed = true;
        public bool OneDirectionRestricted;
        
        public override bool IsPassable(Vector3Int to)
        {
            if (OneDirectionRestricted)
            {
                var v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);
                
                return !(to-position).Equals(v);
            }
            
            return true;
        }

        public override bool IsPassable()
        {
            return OneDirectionRestricted || !closed;
        }

        public override bool IsAtmosPassable()
        {
            return OneDirectionRestricted || !closed;
        }
    }
}