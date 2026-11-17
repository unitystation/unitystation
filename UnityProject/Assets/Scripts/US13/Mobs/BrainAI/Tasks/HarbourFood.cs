using System;
using UnityEngine;
using US13.Core;
using US13.Core.Lifecycle;
using US13.Core.Physics;
using US13.Health;
using US13.HealthV2.Living.BodyParts;
using US13.HealthV2.Living.Metabolism;
using US13.Items.Food;
using US13.Systems.Electricity.FunctionsAndClasses;
using US13.Tilemaps.Behaviours.Layers;
using US13.Tilemaps.Utils;
using Util;

namespace US13.Mobs.BrainAI.Tasks
{



	public sealed class HarbourFoodTask : MobTask
	{
		public override float Range => 1.5f;
		public override int SearchRadius => 5;

		private const int WIRE_SEARCH_RANGE = 2;

		private TargetType targetType = TargetType.None;
		private enum TargetType{
			Food,
			Wire,
			Home,
			None,
		}

		private bool isMoving = false;

		public override bool FindTarget(MobBehaviorContext context, int radius)
		{
			if (targetType == TargetType.Home) return LookForHome(context);
			if ((int)context.Health.GetHungerSystem().CashedHungerState >= (int)HungerState.Hungry)
			{
				if (DMMath.Prob(5) || targetType == TargetType.Wire) return LookForExposedWires(context, WIRE_SEARCH_RANGE); //5% chance to mistake wires for food
				return LookForEdibles(context, radius);
			}
			return false;
		}

		public override void Complete(MobBehaviorContext context)
		{
		}

		private bool LookForEdibles(MobBehaviorContext context, int searchRange)
		{
			var possibleFood =ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject, searchRange, bypassInventories: false);
			if (possibleFood == null) return false;

			Matrix matrix = MatrixOf(context);
			foreach(var food in possibleFood)
			{
				if (food.enabled == false) continue;
				var bodyPart = food.GetComponent<BodyPart>();
				if (bodyPart != null && bodyPart.HealthMaster == context.Health) continue;

				if (Select(context, food.gameObject.AssumedWorldPosServer().ToLocalInt(matrix), matrix,
					    food.gameObject))
				{
					targetType = TargetType.Food;
					return true;
				}

			}
			return false;
		}

		private bool LookForHome(MobBehaviorContext context)
		{
			Matrix matrix = MatrixOf(context);
			if (Select(context, context.HomeCell, matrix, null)) return true; //Try return home, else standstill and eat
			return Select(context, context.AI.Body.RegisterTile.LocalPositionServer, matrix, null);
		}

		private static readonly Func<MobBehaviorContext, Vector3Int, bool> isExposedWire =
			(context, cell) => MatrixOf(context).MetaTileMap.GetTile(cell, LayerType.Floors) is null &&
			                   MatrixOf(context).MetaTileMap.GetTile(cell, LayerType.Electrical) is not null &&
			                   MatrixOf(context).MetaTileMap.IsAtmosPassableAt(cell, true);

		private bool LookForExposedWires(MobBehaviorContext context, int searchRange)
		{
			Matrix matrix = MatrixOf(context);

			foreach (Vector3Int cell in GetCellsInRadius(context, searchRange, isExposedWire))
			{
				if (Select(context, cell, matrix, null))
				{
					targetType = TargetType.Wire;
						return true;
				}
			}

			return false;
		}

		private void EatEdibleInRange(MobBehaviorContext context)
		{
			var possibleFood = ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject, Range, bypassInventories: false);
			if (possibleFood == null) return;

			foreach(var food in possibleFood)
			{
				if (food.enabled == false) continue;

				var bodyPart = food.GetComponent<BodyPart>();
				if (bodyPart != null && bodyPart.HealthMaster == context.Health) continue;

				food.TryConsume(context.AI.Body.gameObject, context.AI.Body.gameObject);
			}
		}

		private void BeginFoodDrag(MobBehaviorContext context)
		{
			var possibleFood = ComponentsTracker<Edible>.GetAllNearbyTypesToTarget(context.AI.Body.gameObject, Range, bypassInventories: false);
			if (possibleFood == null) return;

			UniversalObjectPhysics aiPhysics = context.AI.Body.UniversalObjectPhysics;

			foreach(var food in possibleFood)
			{
				if (food.enabled == false) continue;

				var bodyPart = food.GetComponent<BodyPart>();
				if (bodyPart != null && bodyPart.HealthMaster == context.Health) continue;

				var foodPhysics = food.gameObject.GetUniversalObjectPhysics();
				if (foodPhysics == null) continue;

				aiPhysics.PullSet(foodPhysics, false, true);
				return;
			}
		}

		private void EatExposedWiresInRange(MobBehaviorContext context)
		{
			Matrix matrix = MatrixOf(context);

			foreach (Vector3Int cell in GetCellsInRadius(context, (int)Range, isExposedWire))
			{
				var electricalMatrix = matrix.MetaTileMap.Layers[LayerType.Electrical].Matrix;

				// Check if there's cables at this position
				var cables = electricalMatrix.GetElectricalConnections(cell);
				if (cables == null || cables.List.Count < 1) return;

				var cable = cables.List.PickRandom();
				WireChew(context, cable, matrix, cell);
				return;
			}
		}

		private void WireChew(MobBehaviorContext context, IntrinsicElectronicData cable, Matrix matrix, Vector3Int cell)
		{
			ElectricityFunctions.WorkOutActualNumbers(cable);
			float voltage = cable.Data.ActualVoltage;
			var worldPosition = cell.ToWorldInt(matrix);

			// Remove the cable and spawn the item.
			var electricalTile = matrix.MetaTileMap.GetTile(cell, LayerType.Electrical) as ElectricalCableTile;
			if (electricalTile == true)
				Spawn.ServerPrefab(electricalTile.SpawnOnDeconstruct, worldPosition,
					count: electricalTile.SpawnAmountOnDeconstruct);

			cable.DestroyThisPlease();

			Electrocution electrocution = new Electrocution(voltage, worldPosition);
			context.Health.Electrocute(electrocution);
		}

		public override float Begin(MobBehaviorContext context)
		{
			switch (targetType)
			{
				case TargetType.Wire:
					EatExposedWiresInRange(context);
					targetType = TargetType.None;
					break;
				case TargetType.Food:
					BeginFoodDrag(context);
					targetType = TargetType.Home; //drag food back to home location
					break;
				case TargetType.Home:
					context.AI.Body.UniversalObjectPhysics.StopPulling(false); //Stop pulling the food
					EatEdibleInRange(context); //Try eat the nearby food
					targetType = TargetType.None;
					break;
			}

			return 1.0f;
		}
	}
}
