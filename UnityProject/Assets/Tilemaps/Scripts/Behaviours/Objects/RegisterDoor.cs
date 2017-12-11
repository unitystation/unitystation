using Doors;
using Tilemaps.Scripts.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    [ExecuteInEditMode]
    public class RegisterDoor : RegisterObject
    {
        public bool OneDirectionRestricted;

        public bool IsClosed = true;

        public override bool IsPassable(Vector3Int to)
        {
            if (IsClosed && OneDirectionRestricted)
            {
                var v = Vector3Int.RoundToInt(transform.localRotation * Vector3.down);

                return !(to - Position).Equals(v);
            }

            return true;
        }

        public override bool IsPassable()
        {
            return OneDirectionRestricted || !IsClosed;
        }

        public override bool IsAtmosPassable()
        {
            return OneDirectionRestricted || !IsClosed;
        }
    }
}
