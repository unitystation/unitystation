using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpPathFinding.cs
{
    public class Util
    {
        public static DiagonalMovement GetDiagonalMovement(bool iCrossCorners, bool iCrossAdjacentPoint)
        {

            if (iCrossCorners && iCrossAdjacentPoint)
            {
                return DiagonalMovement.Always;
            }
            else if (iCrossCorners)
            {
                return DiagonalMovement.IfAtLeastOneWalkable;
            }
            else
            {
                return DiagonalMovement.OnlyWhenNoObstacles;
            }
        }
    }
}
