using System;
using System.Collections.Generic;
using UnityEngine;
using US13.Managers.NetworkManagement;
using US13.Player.MovementV2;

namespace US13.Mobs.BrainAI.States
{
	public class BrainWanderState : BrainMobState
	{
		private enum TurnDirection
		{
			Left = 1,
			Right = -1,
		}

		protected List<Vector2Int> Directions = new List<Vector2Int>()
		{
			new Vector2Int(1, 0),
			new Vector2Int(0, -1),
			new Vector2Int(-1, 0),
			new Vector2Int(0, 1),
		}; //Organised so going down the array is "turning left", going up is "turning right"

		private int currentFacing = 0; //Index in Directions
		private int directionCounter = 0; //How many updates have I been facing this direction for?

		public override void OnEnterState()
		{
			directionCounter = 0;
			currentFacing = 1;
			Turn(TurnDirection.Right);
			// No Behavior Required
		}

		public override void OnExitState()
		{
			// No Behavior Required
		}

		public override void OnUpdateTick()
		{
			if (HasGoal() == false) return;
			//If another state has an action to perform exit this state

			if (Move(Directions[currentFacing]))
			{
				directionCounter++;
				if (UnityEngine.Random.Range(0, 10 - Math.Max(7, directionCounter)) == 0) Turn(LeftOrRight());
				//If a move was successful, have a chance at rotating that increases with every successful move in that direction. Caps at 33%
				return;
			}

			//If failed to move forward, always try a new direction.
			Turn(LeftOrRight());
		}

		/// <summary>
		/// Picks a direction to rotate.
		/// </summary>
		/// <returns>1 or -1 at a 50% chance for either</returns>
		private TurnDirection LeftOrRight()
		{
			return (TurnDirection)(1 - UnityEngine.Random.Range(0, 2) * 2);
		}

		/// <summary>
		/// Rotates the current move direction of the AI agent
		/// </summary>
		/// <param name="direction">The direction to rotate relative to current facing direction</param>
		private void Turn(TurnDirection direction)
		{
			int attemptedTurn = currentFacing + (int)direction;
			if (attemptedTurn > 3) attemptedTurn = 0;
			if (attemptedTurn < 0) attemptedTurn = 3;

			if (master.Body.Rotatable && attemptedTurn != currentFacing)
			{
				master.Body.Rotatable.SetFaceDirectionLocalVector(Directions[currentFacing]);
			}

			directionCounter = 0;
			currentFacing = attemptedTurn;
		}

		public override bool HasGoal()
		{
			foreach (var state in master.MobStates)
			{
				if (state == this) continue;
				if (state.Blacklist.Contains(this)) continue;
				if (state.HasGoal())
				{
					master.RemoveAddState(this, state);
					return false;
				}
			}
			return true;
		}

		private bool Move(Vector2Int dirToMove)
		{
			var moveData = master.Traversal.Movement.GenerateMoveData(
				master.Traversal.Movement.registerTile.LocalPosition,
				MovementSynchronisation.VectorToPlayerMoveDirection(dirToMove));

			return master.Traversal.Movement.TryMove(ref moveData, gameObject, CustomNetworkManager.IsServer, out var slip);
		}

	}
}
