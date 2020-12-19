﻿using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Items;
using NaughtyAttributes;
using Objects.Construction;
using AddressableReferences;

namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain specifically trained to explore
	/// the surrounding area for specific objects
	/// </summary>
	public class MobExplore : MobAgent
	{

		[SerializeField] private AddressableAudioSource EatFoodA = null;

		//Add your targets as needed
		public enum Target
		{
			food,
			dirtyFloor,
			missingFloor,
			injuredPeople,
			players
		}

		public event Action FoodEatenEvent;

		public Target target;

		[Tooltip("Indicates the time it takes for the mob to perform its main action. If the the time is 0, it means that the action is instantaneous.")]
		[SerializeField]
		private float actionPerformTime = 0.0f;

		[Tooltip("If true, this creature will only eat stuff in the food preferences list.")]
		[SerializeField]
		private bool hasFoodPrefereces = false;

		[Tooltip("Objects in this list are considered food by this creature (even non edible stuff!)")]
		[SerializeField]
		[ShowIf(nameof(hasFoodPrefereces))]
		private List<GameObject> foodPreferences = null;

		private List<string> foodInitialNames = new List<string>();

		// Timer that indicates if the action perform time is reached and the action can be performed.
		private float actionPerformTimer = 0.0f;

		private InteractableTiles _interactableTiles = null;
		// Position at which an action is performed
		protected Vector3Int actionPosition;

		private InteractableTiles interactableTiles {
			get {
				if (_interactableTiles == null)
				{
					_interactableTiles = InteractableTiles.GetAt((Vector2Int)registerObj.LocalPositionServer, true);
				}

				return _interactableTiles;
			}
		}

		public override void Start()
		{
			base.Start();
			if (!hasFoodPrefereces || foodInitialNames.Any())
			{
				return;
			}

			foreach (GameObject food in foodPreferences)
			{
				var initName = food.GetComponent<ItemAttributesV2>()?.InitialName;
				if (initName != null)
				{
					foodInitialNames.Add(initName);
				}
			}
		}

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

		private bool IsTargetFound(Vector3Int checkPos)
		{
			switch (target)
			{
				case Target.food:
					if (hasFoodPrefereces)
						return registerObj.Matrix.Get<ItemAttributesV2>(checkPos, true).Any(IsInFoodPreferences);
					return registerObj.Matrix.GetFirst<Edible>(checkPos, true) != null;
				case Target.dirtyFloor:
					return (registerObj.Matrix.Get<FloorDecal>(checkPos, true).Any(p => p.Cleanable));
				case Target.missingFloor:
					// Checks the topmost tile if its the base layer (below the floor)
					return interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType == LayerType.Base;
				case Target.injuredPeople:
					return false;
				// this includes ghosts!
				case Target.players:
					return registerObj.Matrix.GetFirst<PlayerScript>(checkPos, true) != null;
			}
			return false;
		}

		/// <summary>
		/// Returns true if given food is in this creatures food preferences.
		/// Mobs with no food preferences will return true for any edible object.
		/// </summary>
		/// <param name="food"></param>
		/// <returns></returns>
		public bool IsInFoodPreferences(ItemAttributesV2 food)
		{
			if (!hasFoodPrefereces)
			{
				return food.gameObject.GetComponent<Edible>() != null;
			}

			return foodInitialNames.Contains(food.InitialName);
		}

		/// <summary>
		/// Returns true if given food is in this creatures food preferences.
		/// Mobs with no food preferences will return true for any edible object.
		/// </summary>
		/// <param name="food"></param>
		/// <returns></returns>
		public bool IsInFoodPreferences(GameObject food)
		{
			return IsInFoodPreferences(food.GetComponent<ItemAttributesV2>());
		}

		/// <summary>
		/// Tries  to eat the target, doesn't matter if it is not actually edible.
		/// </summary>
		private void TryEatTarget(Vector3Int checkPos)
		{
			if (hasFoodPrefereces)
			{
				var food = registerObj.Matrix.Get<ItemAttributesV2>(checkPos, true).FirstOrDefault(IsInFoodPreferences);

				if (food == null)
				{
					return;
				}

				// Send the sound to all nearby clients
				SoundManager.PlayNetworkedAtPos(EatFoodA, transform.position, null, false, false, gameObject);

				Despawn.ServerSingle(food.gameObject);
				FoodEatenEvent?.Invoke();
			}
			else
			{
				var food = registerObj.Matrix.GetFirst<Edible>(checkPos, true);

				if (food != null)
				{
					food.TryConsume(gameObject);
				}
			}
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
					TryEatTarget(checkPos);
					break;
				case Target.dirtyFloor:
					var matrixInfo = MatrixManager.AtPoint(checkPos, true);
					var worldPos = MatrixManager.LocalToWorldInt(checkPos, matrixInfo);
					matrixInfo.MetaDataLayer.Clean(worldPos, checkPos, false);
					break;
				case Target.missingFloor:
					interactableTiles.TileChangeManager.UpdateTile(checkPos, TileType.Floor, "Floor");
					break;
				case Target.injuredPeople:
					break;
				case Target.players:
					var people = registerObj.Matrix.GetFirst<PlayerScript>(checkPos, true);
					if (people != null) gameObject.GetComponent<MobAI>().ExplorePeople(people);
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
}
