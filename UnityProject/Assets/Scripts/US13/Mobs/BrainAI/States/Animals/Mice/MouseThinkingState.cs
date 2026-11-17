using System;
using System.Collections.Generic;
using Logs;
using NaughtyAttributes;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using US13.Core;
using US13.Core.Attributes;
using US13.Core.Lifecycle;
using US13.Core.Physics;
using US13.Core.Utils;
using US13.Health;
using US13.HealthV2.Living;
using US13.Items.Food;
using US13.Mobs.Traversal;
using US13.Systems.Electricity.FunctionsAndClasses;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Utils;
using Util;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.SettingsExtensions;

namespace US13.Mobs.BrainAI.States.Animals.Mice
{
	[Serializable, GeneratePropertyBag, NodeDescription(name: "Perform Mob Task", story: "Perform the selected task", category: "Mob/Tasks", id: "euydc1c1a2b34c5d8000000000000005")]
	[RequiresMobContextExtension(typeof(TaskContextExtension))]
	[RequiresMobSettings(typeof(BotTaskSettings))]
	public class MiceThinkingState : BrainMobState
	{
		/*private MobTraversal pathfinder => master.Traversal;
		[SerializeField] private BrainWanderState wanderState;

		[SerializeReference, SelectImplementation(typeof(ITraversalStrat))]
		public List<ITraversalStrat> TraversalStrategies = new List<ITraversalStrat>();

		private MobTraversal.TraversalDetails traversalDetails;
		private bool isTraversing = false;

		private enum MissionType{
			HeadingToPossibleFood,
			HeadingToWire,
			HeadingToHome,
			None,
		}
		private MissionType missionType = MissionType.None;
		private Matrix localMatrix;
		private Vector3Int homeLocation = Vector3Int.zero;

		private const int FOOD_SEARCH_RANGE = 5;

		private const int WIRE_SEARCH_RANGE = 2;

		[SerializeField] private string squeakActionID = "squeak_mouse";

		/* Summary of Mice AI:
		 * While above 75% hunger, simply wander
		 * [A] If below 75% hunger, occasionally search for food in nearby area.
		 * If find food, navigate to it. Then attempt to drag it to home location.
		 * If home location cannot be reached, eat a bite of the food, then return to [A]
		 * If reached home location, eat a bite of the food, then return to [A]
		 *
		 * Mice should have 5% chance of recognising a nearby wire as food.
		 */
		/*
		private void Start()
		{
			traversalDetails = new MobTraversal.TraversalDetails
			{
				OnTraversalFinalStep = () => OnDoneTraversalToLocation(Vector3Int.zero),
				OnRetryMoveToDirection = null,
				Strats = TraversalStrategies,
				CancelOnSlip = true,
				Algorithm = PathfinderType.AStar
			};
		}

		public override void OnRemovedFromBody(LivingHealthMasterBase livingHealth, GameObject source = null)
		{
			UnsubscribeToPathfinderEvents();
			base.OnRemovedFromBody(livingHealth, source);
		}

		public override void OnEnterState()
		{
			if(homeLocation == Vector3Int.zero) homeLocation = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(master.Body.UniversalObjectPhysics.registerTile.Matrix);
			SubscribeToPathfinderEvents();
			isTraversing = false;
		}

		public override void OnExitState()
		{
			UnsubscribeToPathfinderEvents();
		}

		public override void OnUpdateTick()
		{
			if (DMMath.Prob(2)) LivingHealthMaster.playerScript.PlayerButtonedActions.ServerTriggerAction(squeakActionID, Vector2.zero);

			if (IsStillTraversing()) return; //If currently moving to a location, or unable to act, restrict actions.
			if (LivingHealthMaster.IsSoftCrit || LivingHealthMaster.IsCrit || LivingHealthMaster.IsDead)
			{
				isTraversing = false;
				return;
			}

			//Hunger check
			bool isHungry = true;

			if (isHungry)
			{
				localMatrix = master.Body.UniversalObjectPhysics.registerTile.Matrix;
				if (DMMath.Prob(5)) //5% chance to mistake wires for food
				{
					if (LookEatForExposedWires(WIRE_SEARCH_RANGE) == true) return;
				}
				else if (LookEatForEdibles(FOOD_SEARCH_RANGE) == true) return;
			}
			master.RemoveAddState(this, wanderState);
		}

		private bool LookEatForEdibles(int searchRange, bool tryImmediatelyAct = false)
		{
			var possibleFood =ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(master.Body.gameObject, searchRange, bypassInventories: false);
			if (possibleFood == null) return false;

			var currentPosition  = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(localMatrix);

			foreach(var food in possibleFood)
			{
				var worldPos = food.gameObject.AssumedWorldPosServer();
				Vector3Int targetPosition = worldPos.ToLocalInt(localMatrix);

				if (tryImmediatelyAct)
				{
					food.TryConsume(master.Body.gameObject, master.Body.gameObject);
					return true;
				}

				var possiblePath = MobTraversal.GeneratePath(currentPosition, targetPosition, localMatrix, PathfinderType.AStar);
				if (possiblePath == null || possiblePath.Count == 0) continue;
				possiblePath?.RemoveAt(possiblePath.Count - 1);

				isTraversing = pathfinder.QueueMovementGoalFromPath(traversalDetails, possiblePath);

				if (isTraversing)
				{
					missionType = MissionType.HeadingToPossibleFood; //Tells the mice what to do when it gets to the location
					return true;
				}
			}
			return false;
		}

		private bool LookEatForExposedWires(int searchRange, bool tryImmediatelyAct = false)
		{

			var worldPos = master.Body.gameObject.AssumedWorldPosServer();
			var currentPosition = worldPos.ToLocalInt(localMatrix);

			Vector3Int targetPosition = currentPosition;

			for (int y = -searchRange; y <= searchRange; y++)
			{
				for (int x = -searchRange; x <= searchRange; x++)
				{
					var checkPos = currentPosition;
					checkPos.x += x;
					checkPos.y += y;

					if (IsExposedWire(checkPos, localMatrix))
					{
						if (tryImmediatelyAct)
						{
							var metaTileMap = localMatrix.MetaTileMap;
							var electricalMatrix = metaTileMap.Layers[LayerType.Electrical].Matrix;

							// Check if there's cables at this position
							var cables = electricalMatrix.GetElectricalConnections(checkPos);
							if (cables == null || cables.List.Count < 1) return false;

							var cable = cables.List.PickRandom();
							WireChew(cable, metaTileMap, worldPos.CutToInt() + new Vector3Int(x, y, 0));
							return true;
						}

						var possiblePath = MobTraversal.GeneratePath(currentPosition, targetPosition, localMatrix, PathfinderType.AStar);
						if (possiblePath == null || possiblePath.Count == 0) continue;
						possiblePath?.RemoveAt(possiblePath.Count - 1);

						isTraversing = pathfinder.QueueMovementGoalFromPath(traversalDetails, possiblePath);

						if (isTraversing)
						{
							missionType = MissionType.HeadingToWire; //Tells the mice what to do when it gets to the location
							return true;
						}
					}
				}
			}

			return false;
		}

		private void WireChew(IntrinsicElectronicData cable, MetaTileMap metaTileMap, Vector3Int worldPosition)
		{
			ElectricityFunctions.WorkOutActualNumbers(cable);
			float voltage = cable.Data.ActualVoltage;

			// Remove the cable and spawn the item.
			var electricalTile = metaTileMap.GetTile(worldPosition, LayerType.Electrical) as ElectricalCableTile;
			if (electricalTile == true) Spawn.ServerPrefab(electricalTile.SpawnOnDeconstruct, worldPosition, count: electricalTile.SpawnAmountOnDeconstruct);

			cable.DestroyThisPlease();

			Electrocution electrocution = new Electrocution(voltage, worldPosition);
			master.RelatedPart.HealthMaster.Electrocute(electrocution);
		}

		private void OnDoneTraversalToLocation(Vector3Int pos)
		{
			isTraversing = false;
			if(master.IsControlledByPlayer == false) return;

			switch (missionType)
			{
				case MissionType.HeadingToWire:
					LookEatForExposedWires(1, true);
					break;
				case MissionType.HeadingToPossibleFood:
					TryPullFoodToHome();
					if(missionType != MissionType.HeadingToHome) LookEatForEdibles(1, true); //Could not find a way home, eat food now
					break;
				case MissionType.HeadingToHome:
					UniversalObjectPhysics mousePhysics = master.RelatedPart.HealthMaster.ObjectBehaviour;
					mousePhysics.StopPulling(false);
					LookEatForEdibles(1, true); //Try eat the food brought home
					break;
			}

			missionType = MissionType.None;
			if(isTraversing == false) master.RemoveAddState(this, wanderState);
		}

		private void TryPullFoodToHome()
		{
			var possibleFood =ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(master.Body.gameObject, 1, bypassInventories: false);
			if (possibleFood == null) return;

			var currentPosition  = master.Body.gameObject.AssumedWorldPosServer().ToLocalInt(localMatrix);

			foreach(var food in possibleFood)
			{
				UniversalObjectPhysics mousePhysics = master.RelatedPart.HealthMaster.ObjectBehaviour;
				UniversalObjectPhysics foodPhysics = food.GetComponent<UniversalObjectPhysics>();
				if(foodPhysics == null) continue;

				mousePhysics.PullSet(foodPhysics, false);
				break;
			}
			var possiblePath = MobTraversal.GeneratePath(currentPosition, homeLocation, localMatrix, PathfinderType.AStar);
			if (possiblePath == null || possiblePath.Count == 0) return;

			isTraversing = pathfinder.QueueMovementGoalFromPath(traversalDetails, possiblePath);

			if (isTraversing) missionType = MissionType.HeadingToHome; //Tells the mice what to do when it gets to the location
		}

		private static bool IsExposedWire(Vector3Int position, Matrix matrix)
		{
			return matrix.MetaTileMap.GetTile(position, LayerType.Floors) is null &&
			       matrix.MetaTileMap.GetTile(position, LayerType.Electrical) is not null &&
			       matrix.MetaTileMap.IsAtmosPassableAt(position, matrix);
		}

		private bool IsStillTraversing()
		{
			if (pathfinder == false || isTraversing == false) return false;
			if (pathfinder.QueuedTargets != 0) return true;
			isTraversing = false;
			return false;
		}

		private void TraversalFailed(Vector3Int pos)
		{
			isTraversing = false;
			if(master.IsControlledByPlayer == false) return;
			if (missionType == MissionType.HeadingToHome) //If cant bring food home, still try eat it
			{
				UniversalObjectPhysics mousePhysics = master.RelatedPart.HealthMaster.ObjectBehaviour;
				mousePhysics.StopPulling(false);
				LookEatForEdibles(1, true); //Try eat the food brought home
			}
			missionType = MissionType.None;
			master.RemoveAddState(this, wanderState);
		}

		private void SubscribeToPathfinderEvents()
		{
			isTraversing = false;
			if (pathfinder == false) return;
			pathfinder.OnDoneTraversalToLocation += OnDoneTraversalToLocation;
			pathfinder.OnTraversalFailedCompletely += TraversalFailed;

		}

		private void UnsubscribeToPathfinderEvents()
		{
			isTraversing = false;
			if (pathfinder == false) return;
			pathfinder.OnDoneTraversalToLocation -= OnDoneTraversalToLocation;
			pathfinder.OnTraversalFailedCompletely -= TraversalFailed;
		}

		public override bool HasGoal()
		{
			return missionType != MissionType.None;
		}*/
	}
}