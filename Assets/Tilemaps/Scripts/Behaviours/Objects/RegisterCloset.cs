using Cupboards;
using Tilemaps.Scripts.Utils;
using UnityEngine;

namespace Tilemaps.Scripts.Behaviours.Objects
{
    [ExecuteInEditMode]
    public class RegisterCloset : RegisterObject
    {
        public bool IsClosed { get; set; }
        
        public override bool IsPassable()
        {
            return !IsClosed;
        }
    }
}