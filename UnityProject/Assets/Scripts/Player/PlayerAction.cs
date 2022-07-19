using System;
using UnityEngine;

/// <summary>
/// Used for prediction. Holds a player's desired action.
/// </summary>
public struct PlayerAction
{
	/// int values of the moveactions (will have 2 moveActions if it's a diagonal movement)
	public int[] moveActions;

	/// Set to true when client believes this action doesn't make player move
	public bool isBump;

	/// Set to true when client demands to run for given move (instead of walking)
	public bool isRun;

	/// Set to true when client suggests some action that isn't covered by prediction
	public bool isNonPredictive;

	//clone of PlayerMove GetMoveDirection stuff
	//but there should be a way to see the direction of these keycodes ffs
	public Vector2Int Direction()
	{
		Vector2Int direction = Vector2Int.zero;
		for (var i = 0; i < moveActions.Length; i++)
		{
			direction += GetMoveDirection((MoveAction) moveActions[i]);
		}

		direction.x = Mathf.Clamp(direction.x, -1, 1);
		direction.y = Mathf.Clamp(direction.y, -1, 1);

		return direction;
	}



	public MovementSynchronisation.PlayerMoveDirection ToPlayerMoveDirection()
	{
		var direction = Direction();
		return MovementSynchronisation.VectorToPlayerMoveDirection(direction);
	}

	/// <summary>
	/// Gets the move action corresponding to a cardinal (non-diagonal) direction
	/// </summary>
	/// <param name="direction">cardinal direction vector (such as Vector2Int.up)</param>
	/// <returns>the MoveAction corresponding to the direction, undefined behavior if input is not a valid cardinal direction</returns>
	public static MoveAction GetMoveAction(Vector2Int direction)
	{
		//TODO: Refactor diagonality into an extension
		if (Math.Abs(direction.x) + Math.Abs(direction.y) >= 2)
		{
			Logger.LogErrorFormat("MoveAction.GetMoveAction invoked on an invalid, non-cardinal direction {0}." +
			                      " This will cause undefined behavior. Please fix the code to only pass a valid cardinal direction.",
				Category.Movement, direction);
		}

		if (direction == Vector2Int.up)
		{
			return MoveAction.MoveUp;
		}
		else if (direction == Vector2Int.down)
		{
			return MoveAction.MoveDown;
		}
		else if (direction == Vector2Int.left)
		{
			return MoveAction.MoveLeft;
		}
		else
		{
			return MoveAction.MoveRight;
		}
	}

	private static Vector2Int GetMoveDirection(MoveAction action)
	{
		switch (action)
		{
			case MoveAction.MoveUp:
				return Vector2Int.up;
			case MoveAction.MoveLeft:
				return Vector2Int.left;
			case MoveAction.MoveDown:
				return Vector2Int.down;
			case MoveAction.MoveRight:
				return Vector2Int.right;
		}

		return Vector2Int.zero;
	}

	public static PlayerAction None = new PlayerAction();
}