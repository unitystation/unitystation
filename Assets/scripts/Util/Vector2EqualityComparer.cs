using System.Collections.Generic;
using UnityEngine;

class Vector2EqualityComparer : IEqualityComparer<Vector2>
{
    public bool Equals(Vector2 self, Vector2 vector)
    {
        return self.x == vector.x && self.y == vector.y;
    }

    public int GetHashCode(Vector2 obj)
    {
        return obj.x.GetHashCode() ^ obj.y.GetHashCode() << 2;
    }
}