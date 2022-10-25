using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Items;
using Items.Food;
using NaughtyAttributes;
using Objects.Construction;
using AddressableReferences;
using Chemistry;
using Random = System.Random;

namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain specifically trained to explore
	/// the surrounding area for specific objects
	/// </summary>
	public class MobExplore : MobObjective, IServerSpawn
	{
		private AddressableAudioSource eatFoodSound;

		//Add your targets as needed
		public enum Target
		{
			food,
			dirtyFloor,
			missingFloor,
			injuredPeople,
			players,
			none
		}

		public float PriorityBalance = 1;

		[Tooltip("The reagent used by emagged cleanbots")]
		[SerializeField] private Reagent CB_REAGENT;

		public event Action FoodEatenEvent;

		public Target target;

		[Tooltip("Indicates the time it takes for the mob to perform its main action. If the the time is 0, it means that the action is instantaneous.")]
		[SerializeField]
		private float actionPerformTime = 0.0f;

		[Tooltip("If true, this creature will only eat stuff in the food preferences list.")]
		[SerializeField]
		private bool hasFoodPrefereces = false;

		public bool HasFoodPrefereces => hasFoodPrefereces;

		[Tooltip("Objects in this list are considered food by this creature (even non edible stuff!)")]
		[SerializeField]
		[ShowIf(nameof(hasFoodPrefereces))]
		private List<ItemTrait> foodPreferences = null;

		public List<ItemTrait> FoodPreferences => foodPreferences;

		// Timer that indicates if the action perform time is reached and the action can be performed.
		private float actionPerformTimer = 0.0f;

		private InteractableTiles _interactableTiles = null;
		// Position at which an action is performed
		protected Vector3Int actionPosition;

		public bool IsEmagged = false;

		private readonly Random random = new Random();

		private InteractableTiles interactableTiles
		{
			get
			{
				if (_interactableTiles == null)
				{
					_interactableTiles = InteractableTiles.GetAt((Vector2Int)mobTile.LocalPositionServer, true);
				}

				return _interactableTiles;
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			eatFoodSound = CommonSounds.Instance.EatFood;
		}


		/// <summary>
		/// Begin exploring for the given target type
		/// </summary>
		/// <param name="target"></param>
		public void BeginExploring(Target _target)
		{
			target = _target;
		}


		private bool IsTargetFound(Vector3Int checkPos)
		{
			switch (target)
			{
				case Target.food:
					if (hasFoodPrefereces)
						return mobTile.Matrix.Get<ItemAttributesV2>(checkPos, true).Any(IsInFoodPreferences);
					return mobTile.Matrix.GetFirst<Edible>(checkPos, true) != null;

				case Target.dirtyFloor:
					if (IsEmagged == false) return (mobTile.Matrix.Get<FloorDecal>(checkPos, true).Any(p => p.Cleanable));
					else return (mobTile.Matrix.Get<FloorDecal>(checkPos, true).Any(p => p.Cleanable) || (!mobTile.Matrix.Get<FloorDecal>(checkPos, true).Any() && interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType == LayerType.Floors));

				case Target.missingFloor:
					// Checks the topmost tile if its the base or underfloor layer (below the floor)
					if (IsEmagged == false)
					{
						return (interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType == LayerType.Base
					                                || interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType.IsUnderFloor() != null);

					}

					else return interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType == LayerType.Floors;

				case Target.injuredPeople:
					return false;

				// this includes ghosts!
				case Target.players:
					return mobTile.Matrix.GetFirst<PlayerScript>(checkPos, true) != null;

				default:
					return false;
			}
		}

		/// <summary>
		/// Returns true if given food is in this creatures food preferences.
		/// Mobs with no food preferences will return true for any edible object.
		/// </summary>
		/// <param name="food"></param>
		/// <returns></returns>
		public bool IsInFoodPreferences(ItemAttributesV2 food)
		{
			if (hasFoodPrefereces == false)
			{
				return food.gameObject.GetComponent<Edible>() != null;
			}

			return foodPreferences.Any(food.HasTrait);
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
				var food = mobTile.Matrix.Get<ItemAttributesV2>(checkPos, true).FirstOrDefault(IsInFoodPreferences);

				if (food is null)
				{
					return;
				}

				// Send the sound to all nearby clients
				SoundManager.PlayNetworkedAtPos(eatFoodSound, transform.position, sourceObj: gameObject);

				_ = Despawn.ServerSingle(food.gameObject);
				FoodEatenEvent?.Invoke();
			}
			else
			{
				var food = mobTile.Matrix.GetFirst<Edible>(checkPos, true);

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
			if (mobTile == null || mobTile.Matrix == null)
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
					if (IsEmagged) matrixInfo.MetaDataLayer.ReagentReact(new ReagentMix(CB_REAGENT, 5, 283.15f), worldPos, checkPos);
					else matrixInfo.MetaDataLayer.Clean(worldPos, checkPos, false);
					break;
				case Target.missingFloor:
					if (IsEmagged == false) interactableTiles.TileChangeManager.MetaTileMap.SetTile(checkPos, TileType.Floor, "Floor");
					else interactableTiles.TileChangeManager.MetaTileMap.RemoveTileWithlayer(checkPos, LayerType.Floors);

					break;
				case Target.injuredPeople:
					break;
				case Target.players:
					var people = mobTile.Matrix.GetFirst<PlayerScript>(checkPos, true);
					if (people != null) gameObject.GetComponent<MobAI>().ExplorePeople(people);
					break;
			}
		}


		private void StartPerformAction(Vector3Int destination)
		{
			actionPosition = destination;

			OnPerformAction();
		}

		protected void OnPerformAction()
		{
			actionPerformTimer += MobController.UpdateTimeInterval;

			if ((actionPerformTime == 0) || (actionPerformTimer >= actionPerformTime))
			{
				PerformTargetAction(actionPosition);

				actionPerformTimer = 0;
			}
		}


		public override void ContemplatePriority()
		{
			if (IsTargetFound(mobTile.LocalPositionServer))
			{
				Priority += PriorityBalance * 10;
			}
			else
			{
				Priority += PriorityBalance;
			}

		}


		public override void DoAction()
		{
			if (IsTargetFound(mobTile.LocalPositionServer))
			{
				StartPerformAction(mobTile.LocalPositionServer);
			}
			else
			{
				Move(Directions[random.Next(0, Directions.Count)]);
			}
		}
	}
}