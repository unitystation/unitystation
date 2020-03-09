using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI brain specifically trained to explore
/// the surrounding area for specific objects
/// </summary>
public class MobExplore : MobAgent
{
	//Add your targets as needed
	public enum Target
	{
		food,
		dirtyFloor,
		missingFloor,
		injuredPeople
	}

	public Target target;

	[Tooltip("Indicates the time it takes for the mob to perform its main action. If the the time is 0, it means that the action is instantaneous.")]
	[SerializeField]
	private float actionPerformTime = 0.0f;

	// Timer that indicates if the action perform time is reached and the action can be performed.
	private float actionPerformTimer = 0.0f;

	private InteractableTiles _interactableTiles = null;

	private InteractableTiles interactableTiles
	{
		get
		{
			if (_interactableTiles == null)
			{
				_interactableTiles = InteractableTiles.GetAt((Vector2Int)registerObj.LocalPositionServer, true);
			}

			return _interactableTiles;
		}
	}

// Position at which an action is performed
protected Vector3Int actionPosition;

	/// <summary>
	/// Begin searching for the predefined target
	/// </summary>
	public void BeginExploring()
	{
		Activate();
	}

	/// <summary>
	/// Begin exploring for the given target type
	/// </summary>
	/// <param name="target"></param>
	public void BeginExploring(Target _target)
	{
		target = _target;
		Activate();
	}

	public override void CollectObservations()
	{
		var curPos = registerObj.LocalPositionServer;

		ObserveAdjacentTiles();

		//Search surrounding tiles for the target of interest (food, floors to clean, injured people, etc.)
		for (int y = 1; y > -2; y--)
		{
			for (int x = -1; x < 2; x++)
			{
				if (x == 0 && y == 0) continue;

				var checkPos = curPos;
				checkPos.x += x;
				checkPos.y += y;

				if (IsTargetFound(checkPos))
				{
					// Yes the target is here!
					AddVectorObs(true);
				}
				else
				{
					AddVectorObs(false);
				}
			}
		}
	}

	bool IsTargetFound(Vector3Int checkPos)
	{
		switch (target)
		{
			case Target.food:
				if (registerObj.Matrix.GetFirst<Edible>(checkPos, true) != null) return true;
				return false;
			case Target.dirtyFloor:
				return (registerObj.Matrix.Get<FloorDecal>(checkPos, true).Any(p => p.Cleanable));
			case Target.missingFloor:
				// Checks the topmost tile if its the base layer (below the floor)
				return interactableTiles.MetaTileMap.GetTile(checkPos).LayerType == LayerType.Base;
			case Target.injuredPeople:
				return false;
		}
		return false;
	}

	/// <summary>
	/// Override this for custom target actions
	/// </summary>
	protected virtual void PerformTargetAction(Vector3Int checkPos)
	{
		if (registerObj == null || registerObj.Matrix == null)
		{
			return;
		}

		switch (target)
		{
			case Target.food:
				var edible = registerObj.Matrix.GetFirst<Edible>(checkPos, true);
				if(edible != null) edible.NPCTryEat();
				break;
			case Target.dirtyFloor:
				var floorDecal = registerObj.Matrix.Get<FloorDecal>(checkPos, true).FirstOrDefault(p => p.Cleanable);
				if(floorDecal != null) floorDecal.TryClean();
				break;
			case Target.missingFloor:
				interactableTiles.TileChangeManager.UpdateTile(checkPos, TileType.Floor, "Floor");
				break;
			case Target.injuredPeople:
				break;
		}
	}

	public override void AgentAction(float[] vectorAction, string textAction)
	{
		PerformMoveAction(Mathf.FloorToInt(vectorAction[0]));
	}

	protected override void OnPushSolid(Vector3Int destination)
	{
		if (IsTargetFound(destination))
		{
			StartPerformAction(destination);
		}
	}

	private void StartPerformAction(Vector3Int destination)
	{
		performingAction = true;
		actionPosition = destination;

		OnPerformAction();
	}

	protected override void OnPerformAction()
	{
		actionPerformTimer += Time.deltaTime;

		if ((actionPerformTime == 0) || (actionPerformTimer >= actionPerformTime))
		{
			SetReward(1f);
			PerformTargetAction(actionPosition);

			actionPerformTimer = 0;
			performingAction = false;
		}
	}
}