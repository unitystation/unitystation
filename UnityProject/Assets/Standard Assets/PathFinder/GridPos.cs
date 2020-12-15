/*! 
@file GridPos.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/eppathfinding.cs>
@date July 16, 2013
@brief Grid Position Interface
@version 2.0

@section LICENSE

The MIT License (MIT)

Copyright (c) 2013 Woong Gyu La <juhgiyo@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

@section DESCRIPTION

An Interface for the Grid Position Struct.

*/
using System;
using System.Collections.Generic;
using System.Collections;

namespace EpPathFinding.cs
{
    public class GridPos : IEquatable<GridPos>
    {
        public int x;
        public int y;

        public GridPos()
        {
            x = 0;
            y = 0;
        }
        public GridPos(int iX, int iY)
        {
            this.x = iX;
            this.y = iY;
        }

        public GridPos(GridPos b)
        {
            x = b.x;
            y = b.y;
        }

        public override int GetHashCode()
        {
            return x ^ y;
        }

        public override bool Equals(System.Object obj)
        {
            // Unlikely to compare incorrect type so removed for performance
            // if (!(obj.GetType() == typeof(GridPos)))
            //     return false;
            GridPos p = (GridPos)obj;

            if (ReferenceEquals(null, p))
            {
                return false;
            }

            // Return true if the fields match:
            return (x == p.x) && (y == p.y);
        }

        public bool Equals(GridPos p)
        {
            if (ReferenceEquals(null, p))
            {
                return false;
            }
            // Return true if the fields match:
            return (x == p.x) && (y == p.y);
        }

        public static bool operator ==(GridPos a, GridPos b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }
            if (ReferenceEquals(null, a))
            {
                return false;
            }
            if (ReferenceEquals(null, b))
            {
                return false;
            }
            // Return true if the fields match:
            return a.x == b.x && a.y == b.y;
        }

        public static bool operator !=(GridPos a, GridPos b)
        {
            return !(a == b);
        }

        public GridPos Set(int iX, int iY)
        {
            this.x = iX;
            this.y = iY;
            return this;
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", x, y);
        }
    }
}
