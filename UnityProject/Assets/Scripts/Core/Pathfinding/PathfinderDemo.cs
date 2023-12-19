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
			    GameGizmomanager.AddNewLineStatic(null, new Vector3(path[i].x, path[i].y),
				    null, new Vector3(path[i + 1].x, path[i + 1].y), Color.blue, 0.046f);
		    }
		    StartCoroutine(MovePath(path));
	    }
	    else
	    {
		    Debug.Log("no path??");
	    }
    }

    private IEnumerator MovePath(List<Vector3Int> path)
    {
	    if(path == null) yield break;
	    while (path.Count != 0)
	    {
		    yield return WaitFor.Seconds(0.25f);
		    var dir = path[0];
		    var direction = (dir - PlayerManager.LocalPlayerObject.AssumedWorldPosServer()).normalized;
		    PlayerManager.LocalPlayerScript.playerMove.ForceTilePush(direction.CutToInt().To2Int().Normalize(), new List<UniversalObjectPhysics>(), null);
		    Debug.Log($"moving in direction {direction.normalized} to {dir} from {PlayerManager.LocalPlayerObject.AssumedWorldPosServer()} remaining {path.Count}");
		    if (PlayerManager.LocalPlayerObject.AssumedWorldPosServer() == path[0])
		    {
			    path.RemoveAt(0);
		    }
	    }
    }
}
