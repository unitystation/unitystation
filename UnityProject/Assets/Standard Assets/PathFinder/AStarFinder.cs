#if (UNITY_EDITOR || UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE || UNITY_WII || UNITY_IOS || UNITY_IPHONE || UNITY_ANDROID || UNITY_PS4 || UNITY_SAMSUNGTV || UNITY_XBOXONE || UNITY_TIZEN || UNITY_TVOS || UNITY_WP_8_1 || UNITY_WSA || UNITY_WSA_8_1 || UNITY_WSA_10_0 || UNITY_WINRT || UNITY_WINRT_8_1 || UNITY_WINRT_10_0 || UNITY_WEBGL || UNITY_ADS || UNITY_ANALYTICS || UNITY_ASSERTIONS)
#define UNITY
#else
using System.Threading.Tasks;
#endif
using C5;
using System;
using System.Collections;
using System.Collections.Generic;





namespace EpPathFinding.cs
{
    public class AStarParam : ParamBase
    {
        public delegate float HeuristicDelegate(int iDx, int iDy);


        public float Weight;

        public AStarParam(BaseGrid iGrid, GridPos iStartPos, GridPos iEndPos, float iweight, DiagonalMovement iDiagonalMovement = DiagonalMovement.Always, HeuristicMode iMode = HeuristicMode.EUCLIDEAN)
            : base(iGrid,iStartPos,iEndPos, iDiagonalMovement,iMode)
        {
            Weight = iweight;
        }

        public AStarParam(BaseGrid iGrid, float iweight, DiagonalMovement iDiagonalMovement = DiagonalMovement.Always, HeuristicMode iMode = HeuristicMode.EUCLIDEAN)
            : base(iGrid, iDiagonalMovement, iMode)
        {
            Weight = iweight;
        }

        internal override void _reset(GridPos iStartPos, GridPos iEndPos, BaseGrid iSearchGrid = null)
        {

        }
    }
    public static class AStarFinder
    {
        /*
        private class NodeComparer : IComparer<Node>
        {
            public int Compare(Node x, Node y)
            {
                var result = (x.heuristicStartToEndLen - y.heuristicStartToEndLen);
                if (result < 0) return -1;
                else
                if (result > 0) return 1;
                else
                {
                    return 0;
                }
            }
        }
        */
        public static List<GridPos> FindPath(AStarParam iParam)
        {
            object lo = new object();
            //var openList = new IntervalHeap<Node>(new NodeComparer());
            var openList = new IntervalHeap<Node>();
            var startNode = iParam.StartNode;
            var endNode = iParam.EndNode;
            var heuristic = iParam.HeuristicFunc;
            var grid = iParam.SearchGrid;
            var diagonalMovement = iParam.DiagonalMovement;
            var weight = iParam.Weight;


            startNode.startToCurNodeLen = 0;
            startNode.heuristicStartToEndLen = 0;

            openList.Add(startNode);
            startNode.isOpened = true;

            while (openList.Count != 0)
            {
                var node = openList.DeleteMin();
                node.isClosed = true;

                if (node == endNode)
                {
                    return Node.Backtrace(endNode);
                }

                var neighbors = grid.GetNeighbors(node, diagonalMovement);

#if (UNITY)
                foreach(var neighbor in neighbors)
#else
                Parallel.ForEach(neighbors, neighbor =>
#endif
                {
#if (UNITY)
                    if (neighbor.isClosed) continue;
#else
                    if (neighbor.isClosed) return;
#endif
                    var x = neighbor.x;
                    var y = neighbor.y;
                    float ng = node.startToCurNodeLen + (float)((x - node.x == 0 || y - node.y == 0) ? 1 : Math.Sqrt(2));

                    if (!neighbor.isOpened || ng < neighbor.startToCurNodeLen)
                    {
                        neighbor.startToCurNodeLen = ng;
                        if (neighbor.heuristicCurNodeToEndLen == null) neighbor.heuristicCurNodeToEndLen = weight * heuristic(Math.Abs(x - endNode.x), Math.Abs(y - endNode.y));
                        neighbor.heuristicStartToEndLen = neighbor.startToCurNodeLen + neighbor.heuristicCurNodeToEndLen.Value;
                        neighbor.parent = node;
                        if (!neighbor.isOpened)
                        {
                            lock (lo)
                            {
                                openList.Add(neighbor);
                            }
                            neighbor.isOpened = true;
                        }
                        else
                        {

                        }
                    }
                }
#if (!UNITY)
                );
#endif
            }
            return new List<GridPos>();

        }
    }
}
