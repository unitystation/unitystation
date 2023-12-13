namespace Core.Pathfinding
{
    public class Node
    {
        public bool walkable;
        public int gridX;
        public int gridY;

        public int gCost;
        public int hCost;
        public Node parent;

        public Node(int _gridX, int _gridY, bool _walkable)
        {
            walkable = _walkable;
            gridX = _gridX;
            gridY = _gridY;
        }

        public int fCost
        {
            get
            {
                return gCost + hCost;
            }
        }
    }
}