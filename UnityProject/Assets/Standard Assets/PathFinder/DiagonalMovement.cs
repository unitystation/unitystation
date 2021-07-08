using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpPathFinding.cs

{
    public enum DiagonalMovement
    {
        Always,
        Never,
        IfAtLeastOneWalkable,
        OnlyWhenNoObstacles
    }
}
