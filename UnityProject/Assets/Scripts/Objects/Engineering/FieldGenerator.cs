using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Systems.ElectricalArcs;
using ScriptableObjects.Gun;
using UnityEngine;
using Weapons.Projectiles.Behaviours;
using Tiles;

namespace Objects.Engineering
{
	public class FieldGenerator : MonoBehaviour, ICheckedInteractable<HandApply>, IOnHitDetect, IExaminable, IServerSpawn
	{
		[SerializeField]
		private SpriteHandler topSpriteHandler = null;
		[SerializeField]
		private SpriteHandler powerSpriteHandler = null;
		[SerializeField]
		private SpriteHandler healthSpriteHandler = null;

		[SerializeField]
		private AnimatedTile vertical = null;
		[SerializeField]
		private AnimatedTile horizontal = null;

		[SerializeField]
		[Tooltip("electrical shield effect")]
		private GameObject electricalArc = null;

		[SerializeField]
		[Tooltip("Whether this field generator should start wrenched and welded")]
		private bool startSetUp;

		[SerializeField]
		[Tooltip("Whether this field generator should ignore power requirements")]
		private bool alwaysOn;

		[SerializeField]
		[Range(0, 100)]
		private int detectionRange = 8;

		[SerializeField]
		private int maxEnergy = 100;

		[SerializeField]
		[Tooltip("How much energy is lost every second")]
		[Min(0)]
		private int energyLossRate = 1;

		/// <summary>
		/// Having energy means that the field will stay on, shared between connecting generators
		/// Emitters used to increase energy
		/// DO NOT SET DIRECTLY USE SetEnergy() when changing in script
		/// </summary>
		[SerializeField]
		private int energy;
		public int Energy => energy;

		/// <summary>
		/// Gameobject = connectedgenerator, then bool = slave/master
		/// </summary>
		private Dictionary<Direction, Tuple<GameObject, bool>> connectedGenerator = new Dictionary<Direction, Tuple<GameObject, bool>>();

		private bool isWelded;
		private bool isWrenched;
		private bool isOn;

		private Integrity integrity;
		private RegisterTile registerTile;
		private UniversalObjectPhysics objectPhysics;

		[SerializeField]
		private Vector3 arcOffSet = new Vector3(0 ,0.5f, 0);

		#region LifeCycle

		private void Awake()
		{
			integrity = GetComponent<Integrity>();
			registerTile = GetComponent<RegisterTile>();
			objectPhysics = GetComponent<UniversalObjectPhysics>();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(FieldGenUpdate, 1f);
			integrity.OnWillDestroyServer.AddListener(OnDestroySelf);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, FieldGenUpdate);
			integrity.OnWillDestroyServer.RemoveListener(OnDestroySelf);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (startSetUp == false) return;

			isWelded = true;
			isWrenched = true;
			objectPhysics.SetIsNotPushable(true);
		}

		#endregion

		/// <summary>
		/// Field Gen Update loop, runs every 1 second
		/// Server Side Only
		/// </summary>
		private void FieldGenUpdate()
		{
			if(isOn == false && alwaysOn == false) return;

			//Lose energy every second
			SetEnergy(-energyLossRate);

			BalanceEnergy();

			if (energy <= 0 && alwaysOn == false)
			{
				TogglePower(false);
				return;
			}

			DetectGenerators();

			TrySpawnShields();

			DoArcEffects();
		}

		#region Energy

		/// <summary>
		/// This is called via interface when a laser hits an object
		/// </summary>
		/// <param name="damageData"></param>
		public void OnHitDetect(OnHitDetectData data)
		{
			if (data.DamageData.AttackType != AttackType.Laser) return;

			SetEnergy(5);
		}

		/// <summary>
		/// Get all connected energy values, average then set them
		/// </summary>
		private void BalanceEnergy()
		{
			if (connectedGenerator.Count == 0) return;

			var newEnergy = 0;

			foreach (var generator in connectedGenerator.ToArray())
			{
				var genEnergy = generator.Value.Item1.GetComponent<FieldGenerator>().energy;

				if(genEnergy <= 0) continue;

				newEnergy += genEnergy;
			}

			newEnergy += energy;

			if (newEnergy == 0)
			{
				return;
			}

			newEnergy /= connectedGenerator.Count + 1;

			if (newEnergy < 0)
			{
				newEnergy = 0;
			}

			SetEnergy(newEnergy, true);

			foreach (var generator in connectedGenerator.ToArray())
			{
				generator.Value.Item1.GetComponent<FieldGenerator>().SetEnergy(newEnergy, true);
			}
		}

		/// <summary>
		/// Use when changing energy values
		/// </summary>
		public void SetEnergy(int energyChange, bool overrideCurrentEnergy = false)
		{
			if (overrideCurrentEnergy)
			{
				energy = energyChange;
				SetEnergySprite();
				return;
			}

			if (energy + energyChange >= maxEnergy)
			{
				energy = maxEnergy;
				SetEnergySprite();
				return;
			}

			if (energy + energyChange < maxEnergy)
			{
				energy += energyChange;
				SetEnergySprite();
				return;
			}

			if (energy < 0)
			{
				energy = 0;
				SetEnergySprite();
			}
		}

		private void SetEnergySprite()
		{
			if (energy <= 0)
			{
				topSpriteHandler.PushClear();
				return;
			}

			if (energy <= maxEnergy / 3)
			{
				powerSpriteHandler.ChangeSprite(0);
			}
			else if (energy <= maxEnergy / 1.5)
			{
				powerSpriteHandler.ChangeSprite(1);
			}
			else
			{
				powerSpriteHandler.ChangeSprite(2);
			}
		}

		#endregion

		#region DetectGenerators

		/// <summary>
		/// Detect generators
		/// </summary>
		private void DetectGenerators()
		{
			var enumValues = Enum.GetValues(typeof(Direction));

			foreach (var value in enumValues)
			{
				if (connectedGenerator.ContainsKey((Direction)value)) continue;

				for (int i = 1; i <= detectionRange; i++)
				{
					var pos = registerTile.WorldPositionServer + GetCoordFromDirection((Direction)value) * i;

					var objects = MatrixManager.GetAt<FieldGenerator>(pos, true) as List<FieldGenerator>;

					if (objects == null) continue;

					//If there isn't a field generator and it is impassable dont check further
					if (objects.Count == 0 && MatrixManager.IsPassableAtAllMatricesOneTile(pos, true, false) == false)
					{
						break;
					}

					if (objects.Count <= 0 || objects[0].isWelded == false) continue;

					//Shouldn't be more than one, but just in case pick first
					//Add to connected gen dictionary
					connectedGenerator.Add((Direction)value, new Tuple<GameObject, bool>(objects[0].gameObject, false));
					objects[0].integrity.OnWillDestroyServer.AddListener(OnConnectedDestroy);
					break;
				}
			}
		}

		#endregion

		#region SpawnShields

		private void TrySpawnShields()
		{
			if(energy < maxEnergy / 3) return;

			foreach (var generator in connectedGenerator.ToArray())
			{
				if (generator.Value.Item2 == false)
				{
					var coords = new List<Vector3Int>();
					bool passCheck = false;

					for (int i = 1; i <= detectionRange; i++)
					{
						var pos = registerTile.WorldPositionServer + GetCoordFromDirection(generator.Key) * i;

						if (pos == generator.Value.Item1.AssumedWorldPosServer())
						{
							passCheck = true;
							break;
						}

						//If it is impassable dont check further
						if (!MatrixManager.IsPassableAtAllMatricesOneTile(pos, true, false))
						{
							break;
						}

						coords.Add(pos);
					}

					if (passCheck == false) continue;

					foreach (var coord in coords)
					{
						var matrix = MatrixManager.AtPoint(coord, true);

						matrix.TileChangeManager.MetaTileMap.SetTile(MatrixManager.WorldToLocalInt(coord, matrix), GetTileFromDirection(generator.Key));
					}

					connectedGenerator[generator.Key] = new Tuple<GameObject, bool>(generator.Value.Item1, true);

					var field = generator.Value.Item1.GetComponent<FieldGenerator>();

					topSpriteHandler.ChangeSprite(0);
					field.topSpriteHandler.ChangeSprite(0);
					field.TogglePower(true);
					field.SetEnergy(10);
					SetEnergy(-10);
				}
			}
		}

		#endregion

		#region OnDestroy

		private void OnDestroySelf(DestructionInfo info)
		{
			RemoveAllShields();
		}

		private void RemoveAllShields()
		{
			foreach (var generator in connectedGenerator.ToArray())
			{
				InternalRemoveGenerator(generator.Value.Item1, generator.Key);

				generator.Value.Item1.GetComponent<Integrity>().OnWillDestroyServer.RemoveListener(OnConnectedDestroy);

				//Tell connected generator to remove us
				generator.Value.Item1.GetComponent<FieldGenerator>().RemoveGenerator(gameObject, removeConnected: false);
			}

			connectedGenerator.Clear();
		}

		private void OnConnectedDestroy(DestructionInfo info)
		{
			RemoveGenerator(info.Destroyed.gameObject, info.Destroyed);
		}

		private void RemoveGenerator(GameObject generatorToRemove, Integrity generatorIntegrity = null, bool removeConnected = true)
		{
			foreach (var generator in connectedGenerator.ToArray())
			{
				if (generator.Value.Item1 != generatorToRemove.gameObject) continue;

				InternalRemoveGenerator(generatorToRemove, generator.Key);

				if (generatorIntegrity == null)
				{
					generatorIntegrity = generatorToRemove.GetComponent<Integrity>();
				}

				connectedGenerator.Remove(generator.Key);
				generatorIntegrity.OnWillDestroyServer.RemoveListener(OnConnectedDestroy);

				if (removeConnected)
				{
					//Tell connected generator to remove us
					generatorToRemove.GetComponent<FieldGenerator>().RemoveGenerator(gameObject, removeConnected: false);
				}
				break;
			}
		}

		private void InternalRemoveGenerator(GameObject generatorToRemove, Direction direction)
		{
			for (int i = 1; i <= detectionRange; i++)
			{
				var pos = registerTile.WorldPositionServer + GetCoordFromDirection(direction) * i;

				if (pos == generatorToRemove.AssumedWorldPosServer())
				{
					break;
				}

				var matrix = MatrixManager.AtPoint(pos, true);

				var layerTile = matrix.TileChangeManager.MetaTileMap
					.GetTile(MatrixManager.WorldToLocalInt(pos, matrix), LayerType.Walls);

				if (layerTile == null) continue;

				if (layerTile.name == horizontal.name || layerTile.name == vertical.name)
				{
					matrix.TileChangeManager.MetaTileMap.RemoveTileWithlayer(MatrixManager.WorldToLocalInt(pos, matrix), LayerType.Walls);
				}
			}
		}

		#endregion

		#region DirectionMethods

		private AnimatedTile GetTileFromDirection(Direction direction)
		{
			switch (direction)
			{
				case Direction.Up:
					return vertical;
				case Direction.Down:
					return vertical;
				case Direction.Left:
					return horizontal;
				case Direction.Right:
					return horizontal;
				default:
					Loggy.LogError($"Somehow got a wrong direction for {gameObject.ExpensiveName()} tile setting", Category.Machines);
					return vertical;
			}
		}

		private Vector3Int GetCoordFromDirection(Direction direction)
		{
			switch (direction)
			{
				case Direction.Up:
					return Vector3Int.up;
				case Direction.Down:
					return Vector3Int.down;
				case Direction.Left:
					return Vector3Int.left;
				case Direction.Right:
					return Vector3Int.right;
				default:
					Loggy.LogError($"Somehow got a wrong direction for {gameObject.ExpensiveName()}", Category.Machines);
					return Vector3Int.zero;
			}
		}

		private Direction GetOppositeDirection(Direction direction)
		{
			switch (direction)
			{
				case Direction.Up:
					return Direction.Down;
				case Direction.Down:
					return Direction.Up;
				case Direction.Left:
					return Direction.Right;
				case Direction.Right:
					return Direction.Left;
				default:
					Loggy.LogError($"Somehow got wrong opposite direction for {gameObject.ExpensiveName()}", Category.Machines);
					return Direction.Up;
			}
		}

		private enum Direction
		{
			Up,
			Down,
			Left,
			Right
		}

		#endregion

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder)) return true;

			if (interaction.HandObject == null) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				TryWrench(interaction);
			}
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder))
			{
				TryWeld(interaction);
			}
			else
			{
				TryToggleOnOff(interaction);
			}
		}

		#endregion

		#region Power

		private void TryToggleOnOff(HandApply interaction)
		{
			if (energy <= 0)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs energy before it can be turned on");
				return;
			}

			if (isOn)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter will only shutdown when it has no energy");
			}
			else if (isWelded)
			{
				Chat.AddActionMsgToChat(interaction.Performer, "You turn the field generator on",
					$"{interaction.Performer.ExpensiveName()} turns the field generator on");

				TogglePower(true);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be wrench and welded down first");
			}
		}

		public void TogglePower(bool newIsOn)
		{
			if (newIsOn)
			{
				isOn = true;
			}
			else
			{
				isOn = false;
				RemoveAllShields();
				topSpriteHandler.PushClear();
				powerSpriteHandler.PushClear();
				healthSpriteHandler.PushClear();
				energy = 0;
			}
		}

		#endregion

		#region Weld

		private void TryWeld(HandApply interaction)
		{
			if (isOn)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The field generator needs to be turned off first");
				return;
			}

			if (isWrenched == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The field generator needs to be wrenched down first");
				return;
			}

			if (!interaction.HandObject.GetComponent<Welder>().IsOn)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You need a fueled and lit welder");
				return;
			}

			if (isWelded)
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					"You start to unweld the field generator...",
					$"{interaction.Performer.ExpensiveName()} starts to unweld the field generator...",
					"You unweld the field generator from the floor.",
					$"{interaction.Performer.ExpensiveName()} unwelds the field generator from the floor.",
					() =>
					{
						isWelded = false;
						TogglePower(false);
					});
			}
			else
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					"You start to weld the field generator...",
					$"{interaction.Performer.ExpensiveName()} starts to weld the field generator...",
					"You weld the field generator to the floor.",
					$"{interaction.Performer.ExpensiveName()} welds the field generator to the floor.",
					() => { isWelded = true; });
			}
		}

		#endregion

		#region Wrench

		private void TryWrench(HandApply interaction)
		{
			if (isWrenched && isWelded)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be unwelded first");
			}
			else if (isWrenched)
			{
				//unwrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1,
					"You start to wrench the emitter...",
					$"{interaction.Performer.ExpensiveName()} starts to wrench the emitter...",
					"You wrench the emitter off the floor.",
					$"{interaction.Performer.ExpensiveName()} wrenches the emitter off the floor.",
					() =>
					{
						isWrenched = false;
						objectPhysics.SetIsNotPushable(false);
						TogglePower(false);
					});
			}
			else
			{
				if (MatrixManager.IsSpaceAt(registerTile.WorldPositionServer, true, registerTile.Matrix.MatrixInfo))
				{
					Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be on a floor or plating");
					return;
				}

				//wrench
				ToolUtils.ServerUseToolWithActionMessages(interaction, 1,
					"You start to wrench the emitter...",
					$"{interaction.Performer.ExpensiveName()} starts to wrench the emitter...",
					"You wrench the emitter onto the floor.",
					$"{interaction.Performer.ExpensiveName()} wrenches the emitter onto the floor.",
					() =>
					{
						isWrenched = true;
						objectPhysics.SetIsNotPushable(true);
					});
			}
		}

		#endregion

		#region ArcEffect

		private void DoArcEffects()
		{
			foreach (var generator in connectedGenerator)
			{
				if(generator.Value.Item2 == false) continue;

				var settings = new ElectricalArcSettings(electricalArc, gameObject, generator.Value.Item1,
					arcOffSet, arcOffSet, 1, 1f, false, false);

				ElectricalArc.ServerCreateNetworkedArcs(settings);
			}
		}

		#endregion

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"Status: {isOn}\nEnergy: {energy}";
		}
	}
}
