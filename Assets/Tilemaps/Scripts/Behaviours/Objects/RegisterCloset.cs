using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    [ExecuteInEditMode]
    public class RegisterCloset : RegisterObject
    {
        public bool closed = true;
        
        public override bool IsPassable()
        {
            return !closed;
        }
    }
}