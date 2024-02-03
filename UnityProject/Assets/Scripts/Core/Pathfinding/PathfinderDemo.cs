using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Core.Pathfinding;
using TileManagement;
using UnityEngine.Tilemaps;

public class PathfinderDemo : MonoBehaviour
{
	public MetaTileMap Walls;

	private List<Vector3Int> path = new List<Vector3Int>();

	private void Update()
    {
	    if (Input.GetKey(KeyCode.F) || Input.GetMouseButtonDown(1))
	    {
		    PathfindTest();
	    }
    }

    public void PathfindTest()
    {
	    Vector3Int endPoint = Walls.MousePositionToCell();
	    Vector3Int startPoint = PlayerManager.LocalPlayerObject.AssumedWorldPosServer().CutToInt();
	    GameGizmomanager.AddNewLineStatic(null, startPoint, null, endPoint, Color.green, 0.031f);

	    path = AStar.FindPathClosest(Walls, startPoint, endPoint);

	    if (path != null && path.Count != 0)
	    {
		    for (int i = 0; i < path.Count - 1; i++)
		    {
			    GameGizmomanager.AddNewLineStatic(null, new Vector3(path[i].x, path[i].y),
				    null, new Vector3(path[i + 1].x, path[i + 1].y), Color.blue, 0.031f);
		    }
		    StartCoroutine(MovePath());
	    }
	    else
	    {
		    Debug.Log("no path??");
	    }
    }

    private IEnumerator MovePath()
    {
	    if(path == null) yield break;
	    var script = PlayerManager.LocalPlayerScript;
	    var tries = 0;
	    while (path.Count != 0)
	    {
		    tries++;
		    yield return WaitFor.Seconds(0.35f);
		    if (path.Count == 0) break;
		    var dir = path[0];
		    var direction = (dir - script.gameObject.AssumedWorldPosServer()).normalized;
		    var move = script.playerMove.NewMoveData(direction.CutToInt().To2Int());
		    script.playerMove.TryMove(ref move, script.gameObject, true, out var slip);
		    Debug.Log($"moving in direction {direction.normalized}/{move.GlobalMoveDirection} to " +
		              $"{dir} from {script.gameObject.AssumedWorldPosServer()} remaining {path.Count} " +
		              $"with distance {Vector3.Distance(PlayerManager.LocalPlayerObject.AssumedWorldPosServer(), path[0])} - {tries}/55");
		    if (tries >= 55 || slip) break;
		    if (Vector3.Distance(PlayerManager.LocalPlayerObject.AssumedWorldPosServer(), path[0]) <= 1.15f)
		    {
			    path.RemoveAt(0);
			    tries = 0;
		    }
	    }
    }
}
