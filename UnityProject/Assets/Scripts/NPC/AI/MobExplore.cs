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
using HealthV2;


namespace Systems.MobAIs
{
	/// <summary>
	/// AI brain specifically trained to explore
	/// the surrounding area for specific objects
	/// </summary>
	public class MobExplore : MobObjective, IServerSpawn
	{
		private List<Vector2Int> visionCirclePerimeter = new List<Vector2Int>();
		private List<Vector3Int> visionCircleArea = new List<Vector3Int>();

		[Tooltip("Range of search")]
		[SerializeField]
		public int visionRange = 10;

		private AddressableAudioSource eatFoodSound;

		//Add your targets as needed
		public enum Target
		{
			food,
			dirtyFloor,
			missingFloor,
			injuredPeople,
			players
		}

		public float PriorityBalance = 1;

		[Tooltip("The reagent used by emagged cleanbots")]
		[SerializeField] private Reagent CB_REAGENT;

		[Tooltip("The reagent used by medibots")]
		[SerializeField] private Reagent HEALING_REAGENT;

		[Tooltip("The reagent used by emagged medibots")]
		[SerializeField] private Reagent HARMFUL_REAGENT;

		public event Action FoodEatenEvent;

		public Target target;

		[NonSerialized]
		public Vector3Int targetPos;

		// to actually check if target is valid since vector isnt nullable type and we dont want for mobs to follow 0,0 or target far out of its target range
		[NonSerialized]
		public bool targetIsValid = false;

		[Tooltip("Indicates the time it takes for the mob to perform its main action. If the the time is 0, it means that the action is instantaneous.")]
		[SerializeField]
		private float actionPerformTime = 0.0f;

		[Tooltip("Indicates the time it takes for the mob to forget it's recent patient.")]
		[SerializeField]
		private float patientMemoryTime = 15.0f;

		[Tooltip("Delay before each vision area update.")]
		[SerializeField]
		private float visionUpdateInterval = 0.5f;

		[Tooltip("If true, this creature will only eat stuff in the food preferences list.")]
		[SerializeField]
		private bool hasFoodPrefereces = false;

		[Tooltip("Objects in this list are considered food by this creature (even non edible stuff!)")]
		[SerializeField]
		[ShowIf(nameof(hasFoodPrefereces))]
		private List<ItemTrait> foodPreferences = null;

		//Timer that indicates if the action perform time is reached and the action can be performed.
		private float actionPerformTimer = 0.0f;

		//Timer that indicates if vision area can be updated again.
		private float visionUpdateTimer = 0.0f;

		//Timer that indicates if recent patient can be forgotten
		private float patientTimer = 0.0f;

		private InteractableTiles _interactableTiles = null;
		//Position at which an action is performed
		protected Vector3Int actionPosition;

		public bool IsEmagged = false;

		//So medibot won't inject same patient 10 times in a row
		private PlayerScript recentPatient;

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

		//https://www.geeksforgeeks.org/bresenhams-circle-drawing-algorithm/
		// Function for circle-generation
		// using Bresenham's algorithm
		private void CircleBres(int xc, int yc, int r)
		{
			int x = 0, y = r;
			int d = 3 - 2 * r;
			DrawCircle(xc, yc, x, y);
			while (y >= x)
			{
				// for each pixel we will
				// draw all eight pixels

				x++;

				// check for decision parameter
				// and correspondingly
				// update d, x, y
				if (d > 0)
				{
					y--;
					d = d + 4 * (x - y) + 10;
				}
				else
					d = d + 4 * x + 6;

				DrawCircle(xc, yc, x, y);
			}
		}

		// Function to put Locations
		// at subsequence points
		private void DrawCircle(int xc, int yc, int x, int y)
		{
			visionCirclePerimeter.Add(new Vector2Int(xc + x, yc + y));
			visionCirclePerimeter.Add(new Vector2Int(xc - x, yc + y));
			visionCirclePerimeter.Add(new Vector2Int(xc + x, yc - y));
			visionCirclePerimeter.Add(new Vector2Int(xc - x, yc - y));
			visionCirclePerimeter.Add(new Vector2Int(xc + y, yc + x));
			visionCirclePerimeter.Add(new Vector2Int(xc - y, yc + x));
			visionCirclePerimeter.Add(new Vector2Int(xc + y, yc - x));
			visionCirclePerimeter.Add(new Vector2Int(xc - y, yc - x));
		}

		//https://rosettacode.org/wiki/Bitmap/Bresenham%27s_line_algorithm#C.23
		private void FillCircle(int xc, int yc, int z)
		{
			foreach (Vector3Int pos in visionCirclePerimeter)
			{
				int dx = Math.Abs(pos.x - xc), sx = xc < pos.x ? 1 : -1;
				int dy = Math.Abs(pos.y - yc), sy = yc < pos.y ? 1 : -1;
				int err = (dx > dy ? dx : -dy) / 2, e2;
				for (; ; )
				{
					visionCircleArea.Add(new Vector3Int(xc, yc, z));
					if (xc == pos.x && yc == pos.y) break;
					e2 = err;
					if (e2 > -dx) { err -= dy; xc += sx; }
					if (e2 < dy) { err += dx; yc += sy; }
				}
			}
		}

		//refreshes vision area
		private void UpdateVisionArea()
		{
			visionCircleArea.Clear();
			visionCirclePerimeter.Clear();
			int xc = mobTile.WorldPositionServer.x;
			int yc = mobTile.WorldPositionServer.y;
			int z = mobTile.WorldPositionServer.z;
			CircleBres(xc, yc, visionRange);
			FillCircle(xc, yc, z);
		}

		/// <summary>
		/// Begin exploring for the given target type
		/// </summary>
		/// <param name="target"></param>
		public void BeginExploring(Target _target)
		{
			target = _target;
		}

		//gets nearest available target
		private void RefreshTargetPos()
		{
			if (visionCircleArea.Count < 1)
			{
				UpdateVisionArea();
			}

			float minDist = 999;
			foreach (Vector3Int worldPos in visionCircleArea)
			{
				if (IsTargetFound(worldPos.ToNonInt3().ToLocalInt(mobTile.Matrix)))
				{
					float curDist = Vector3.Distance(worldPos, mobTile.WorldPositionServer);
					if (curDist < minDist)
					{
						minDist = curDist;
						targetPos = worldPos;
						targetIsValid = true;
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
						return mobTile.Matrix.Get<ItemAttributesV2>(checkPos, true).Any(IsInFoodPreferences);
					return mobTile.Matrix.GetFirst<Edible>(checkPos, true) != null;

				case Target.dirtyFloor:
					if (IsEmagged == false) return (mobTile.Matrix.Get<FloorDecal>(checkPos, true).Any(p => p.Cleanable));
					else return (mobTile.Matrix.Get<FloorDecal>(checkPos, true).Any(p => p.Cleanable) || (!mobTile.Matrix.Get<FloorDecal>(checkPos, true).Any() && interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType == LayerType.Floors));

				case Target.missingFloor:
					if (IsEmagged == false) return (interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType == LayerType.Base || interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType == LayerType.Underfloor); // Checks the topmost tile if its the base or underfloor layer (below the floor)
					else return interactableTiles.MetaTileMap.GetTile(checkPos)?.LayerType == LayerType.Floors;

				case Target.injuredPeople:
					PlayerScript player = mobTile.Matrix.GetFirst<PlayerScript>(checkPos, true);
					if (player != null && player != recentPatient)
					{
						if (!IsEmagged) return player.playerHealth.OverallHealth < 75;
						else return true;
					}
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
					var patient = mobTile.Matrix.GetFirst<PlayerScript>(checkPos, true);
					if (patient != null)
					{
						if (!IsEmagged) patient.playerHealth.CirculatorySystem.BloodPool.Add(new ReagentMix(HEALING_REAGENT, 5, 283.15f));
						else patient.playerHealth.CirculatorySystem.BloodPool.Add(new ReagentMix(HARMFUL_REAGENT, 5, 283.15f));
					}
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
			actionPerformTimer += Time.deltaTime;

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
			if (targetIsValid)
			{
				Priority += PriorityBalance * 5;
			}
			else
			{
				Priority += PriorityBalance;
			}

		}

		public bool MatchesMobPos(Vector3Int pos)
		{
			return pos == mobTile.WorldPositionServer;
		}

		public bool MatchesCurrentTarget(Vector3Int pos)
		{
			return pos == targetPos;
		}

		public override void DoAction()
		{
			visionUpdateTimer += Time.deltaTime;

			if (visionUpdateTimer >= visionUpdateInterval)
			{
				UpdateVisionArea();

				visionUpdateTimer = 0;
			}

			patientTimer += Time.deltaTime;

			if (patientTimer >= patientMemoryTime)
			{
				recentPatient = null;

				visionUpdateTimer = 0;
			}


			if (IsTargetFound(mobTile.LocalPositionServer))
			{
				StartPerformAction(mobTile.LocalPositionServer);
				Vector3Int oldPos = targetPos;
				RefreshTargetPos();
				if (oldPos == targetPos) //target is reached but no new targets found, so its not valid anymore
				{
					targetIsValid = false;
				}
			}
			else
			{
				if (Vector3.Distance(targetPos, mobTile.WorldPositionServer) > visionRange * 3)
				{
					Vector3Int oldPos = targetPos;
					RefreshTargetPos();
					if (oldPos == targetPos) //target is out of range but no new targets found, so its not valid anymore
					{
						targetIsValid = false;
					}
				}

				if (targetPos != null && targetIsValid)
				{
					var moveToRelative = (targetPos - mobTile.WorldPositionServer).ToNonInt3();
					moveToRelative.Normalize();
					var stepDirectionWorld = ChooseDominantDirection(moveToRelative);
					var moveTo = mobTile.WorldPositionServer + stepDirectionWorld;
					var localMoveTo = moveTo.ToLocal(mobTile.Matrix).RoundToInt();

					var distance = Vector3.Distance(targetPos, mobTile.WorldPositionServer);
					if (distance > 2)
					{
						if (mobTile.Matrix.MetaTileMap.IsPassableAtOneTileMap(mobTile.LocalPositionServer, localMoveTo, true))
						{
							Move(stepDirectionWorld);
						}
						else
						{
							Move(Directions.PickRandom());
						}
					}
					else
					{
						Move(stepDirectionWorld);
					}
				}
				else
				{
					Move(Directions.PickRandom());
				}

				RefreshTargetPos();
			}
		}
	}
}