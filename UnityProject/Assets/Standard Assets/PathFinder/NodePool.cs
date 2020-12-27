/*! 
@file NodePool.cs
@author Woong Gyu La a.k.a Chris. <juhgiyo@gmail.com>
		<http://github.com/juhgiyo/eppathfinding.cs>
@date July 16, 2013
@brief NodePool Interface
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

An Interface for the NodePool Class.

*/
using System;
using System.Collections.Generic;
using System.Collections;

namespace EpPathFinding.cs
{
    public class NodePool
    {
        protected Dictionary<GridPos, Node> m_nodes;

        public NodePool()
        {
            m_nodes = new Dictionary<GridPos, Node>();
        }

        public Dictionary<GridPos, Node> Nodes
        {
            get { return m_nodes; }
        }
        public Node GetNode(int iX, int iY)
        {
            GridPos pos = new GridPos(iX, iY);
            return GetNode(pos);
        }

        public Node GetNode(GridPos iPos)
        {
            Node retVal = null;
            m_nodes.TryGetValue(iPos, out retVal);
            return retVal;
        }

        public Node SetNode(int iX, int iY, bool? iWalkable = null)
        {
            GridPos pos = new GridPos(iX, iY);
            return SetNode(pos, iWalkable);
        }

        public Node SetNode(GridPos iPos, bool? iWalkable = null)
        {
            if (iWalkable.HasValue)
            {
                if (iWalkable.Value == true)
                {
                    Node retVal = null;
                    if (m_nodes.TryGetValue(iPos, out retVal))
                    {
                        return retVal;
                    }
                    Node newNode = new Node(iPos.x, iPos.y, iWalkable);
                    m_nodes.Add(iPos, newNode);
                    return newNode;
                }
                else
                {
                    removeNode(iPos);
                }

            }
            else
            {
                Node newNode = new Node(iPos.x, iPos.y, true);
                m_nodes.Add(iPos, newNode);
                return newNode;
            }
            return null;
        }
        protected void removeNode(int iX, int iY)
        {
            GridPos pos = new GridPos(iX, iY);
            removeNode(pos);
        }
        protected void removeNode(GridPos iPos)
        {
            if (m_nodes.ContainsKey(iPos))
                m_nodes.Remove(iPos);
        }
    }
}