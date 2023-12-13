using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Pathfinding;

public class PathfinderDemo : MonoBehaviour
{
	public Grid TileGrid;
    public Pathfinder pathFinder;

    private void Start()
    {
	    pathFinder = new Pathfinder(TileGrid, LayerType.Walls, LayerType.Underfloor);
    }

    private void Update()
    {
	    if (Input.GetKey(KeyCode.F) == false) return;
	    Vector3 mousePos = MouseUtils.MouseToWorldPos();
	    Vector3Int endPoint = TileGrid.WorldToCell(mousePos);
	    Vector3Int startPoint = PlayerManager.LocalPlayerObject.AssumedWorldPosServer().CutToInt();

	    if (pathFinder == null)
	    {
		    pathFinder = new Pathfinder(TileGrid, LayerType.Walls, LayerType.Underfloor);
	    }

	    List<Vector3Int> path = pathFinder.FindPath(startPoint, endPoint);

	    if (path != null)
	    {
		    for (int i = 0; i < path.Count - 1; i++)
		    {
			    Debug.DrawLine(new Vector3(path[i].x, path[i].y), new Vector3(path[i + 1].x, path[i + 1].y), Color.blue,
				    1000f);
		    }
	    }
	    else
	    {
		    Debug.Log("no path??");
	    }
    }
}
