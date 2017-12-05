using Cupboards;
using Tilemaps.Scripts.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    [ExecuteInEditMode]
    public class RegisterCloset : RegisterObject
    {
        public bool IsClosed = true;

        public override bool IsPassable()
        {
            return !IsClosed;
        }
    }
}