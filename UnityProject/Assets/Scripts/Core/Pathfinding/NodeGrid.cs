using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Core.Pathfinding
{
    public class NodeGrid
    {
	    public Grid tileGrid;
	    public LayerType wallTag = LayerType.Walls;
	    public LayerType floorTag = LayerType.Floors;
	    public Vector2Int vGridWorldSize;
	    public Node[,] nodeArray;

	    public int xOffset;
	    public int yOffset;

        public NodeGrid(Grid _TileGrid, LayerType _wallTag, LayerType _FloorTag)
        {
            tileGrid = _TileGrid;
            wallTag = _wallTag;
            floorTag = _FloorTag;
            if (tileGrid != null && wallTag != LayerType.None) CreateGrid();
        }

        public void CreateGrid()
        {
	        // Get all tilemaps
	        Tilemap[] allTileMaps = tileGrid.GetComponentsInChildren<Tilemap>();
	        // Check for largest tilemap
	        int largestX = 0, largestY = 0;
	        foreach (Tilemap map in allTileMaps)
	        {
		        map.CompressBounds();
		        BoundsInt bounds = map.cellBounds;
		        if (bounds.size.x > largestX)
			        largestX = map.size.x;
		        if (bounds.size.y > largestY)
			        largestY = map.size.y;
		        if (map.cellBounds.xMin < xOffset)
			        xOffset = map.cellBounds.xMin;
		        if (map.cellBounds.yMin < yOffset)
			        yOffset = map.cellBounds.yMin;
	        }
	        // Setup variables
	        nodeArray = new Node[largestX, largestY];
	        vGridWorldSize.x = largestX;
	        xOffset *= -1;
	        yOffset *= -1;
	        vGridWorldSize.y = largestY;

            // Add nodes
            foreach (Tilemap map in allTileMaps)
            {
	            foreach (var pos in map.cellBounds.allPositionsWithin)
	            {
		            var layer = map.GetComponent<Layer>().LayerType;
		            if (layer is LayerType.Effects or LayerType.None or LayerType.Objects) continue;
		            int gridPosX = pos.x + xOffset;
		            int gridPosY = pos.y + yOffset;
                    // Make new node
                    if (nodeArray[gridPosX, gridPosY] == null)
                    {
	                    // Check tag
	                    if (layer == wallTag)
	                    {
		                    nodeArray[gridPosX, gridPosY] = new Node(gridPosX, gridPosY, false);
	                    }
	                    else if (layer == floorTag && map.GetTile(new Vector3Int(pos.x, pos.y, 0)) != null)
	                    {
		                    nodeArray[gridPosX, gridPosY] = new Node(gridPosX, gridPosY, true);
	                    }
                    }
                    // Check existing node
                    else if (layer == wallTag || nodeArray[gridPosX, gridPosY].walkable == false || layer != floorTag)
                    {
	                    nodeArray[gridPosX, gridPosY].walkable = false;
                    }
	            }
            }
        }
        public List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;

                    if (checkX >= 0 && checkX < vGridWorldSize.x && checkY >= 0 && checkY < vGridWorldSize.y)
                    {
                        if (nodeArray[checkX, checkY] != null && nodeArray[checkX, checkY].walkable)
                            neighbours.Add(nodeArray[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }
    }
}
