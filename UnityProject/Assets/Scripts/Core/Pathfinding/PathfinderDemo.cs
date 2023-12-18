using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Pathfinding;
using UnityEngine.Tilemaps;

public class PathfinderDemo : MonoBehaviour
{
	public Tilemap Walls;

	private void Update()
    {
	    if (Input.GetKey(KeyCode.F) || Input.GetMouseButtonDown(1))
	    {
		    Debug.Log("clicked, testing.");
		    PathfindTest();
	    }
    }

    public void PathfindTest()
    {
	    Vector3Int endPoint = MouseUtils.MouseToWorldPos().CutToInt();
	    Vector3Int startPoint = PlayerManager.LocalPlayerObject.AssumedWorldPosServer().CutToInt();

	    List<Vector3Int> path = AStar.FindPath(Walls, startPoint, endPoint);

	    if (path != null && path.Count != 0)
	    {
		    for (int i = 0; i < path.Count - 1; i++)
		    {
			    Debug.DrawLine(new Vector3(path[i].x, path[i].y), new Vector3(path[i + 1].x, path[i + 1].y), Color.blue,
				    5f);
		    }
		    PlayerManager.LocalPlayerObject.transform.position = path[^1];
	    }
	    else
	    {
		    Debug.Log("no path??");
	    }
    }
}
