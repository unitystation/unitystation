using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using Light2D;
using HealthV2;
using Systems.Pipes;
using Systems.Radiation;
using Systems.Explosions;
using Objects.Engineering;
using Weapons.Projectiles.Behaviours;
using Random = UnityEngine.Random;
using Tiles;


namespace Objects
{
	public class Singularity : NetworkBehaviour, IOnHitDetect, IExaminable
	{

		[SyncVar(hook = nameof(SyncCurrentStage))]
		private SingularityStages currentStage = SingularityStages.Stage0;

		private readonly float updateFrequency = 0.5f;

		private Orientation currentFacing = Orientation.Up;
		private const float directionThreshold = 93.5f;
		//Chance in % that the singulo will stay its current course each move action.
		//With this value, the singularity will move in a straight line for 10tiles 50% of the time.
		//I thought that was a good compromise between moving in straight lines and still changing direction at random.

		public SingularityStages CurrentStage
		{
			get
			{
				return currentStage;
			}
			set
			{
				currentStage = value;

				UpdateVectors();
				spriteHandler.ChangeSprite((int)currentStage);
			}
		}

		[SyncVar(hook = nameof(SyncLightVector))]
		private Vector3 lightVector = Vector3.zero;

		[SyncVar(hook = nameof(SyncDynamicScale))]
		private Vector3 dynamicScale = Vector3.one;

		[SerializeField]
		private SingularityStages startingStage = SingularityStages.Stage0;

		[SerializeField]
		private int singularityPoints = 100;

		[SerializeField]
		private int maximumPoints = 3250;

		[SerializeField]
		private int pointLossRate = 4;

		[SerializeField]
		private float maxRadiation = 5000f;

		[SerializeField]
		private GameObject WarpEffectFront;

		[SerializeField]
		private GameObject WarpEffectBack;

		[SerializeField]
		private new LightSprite light = null;

		[SerializeField]
		[Tooltip("Allows singularity to be stage 6")]
		private bool eatenSuperMatter;

		[SerializeField]
		[Tooltip("If true singularity wont go down a stage if it loses points")]
		private bool preventDowngrade = false;

		[SerializeField]
		[Tooltip("If true singularity wont die at 0 points")]
		private bool zeroPointDeath = true;

		[SerializeField]
		private LayerTile vertical = null;
		[SerializeField]
		private LayerTile horizontal = null;

		private Transform lightTransform;
		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private UniversalObjectPhysics ObjectPhysics;
		private int objectId;

		private BoxCollider2D boxCollider2D;

		private Material WarpEffectFrontMat;
		private Material WarpEffectBackMat;

		private int lockTimer;
		private bool pointLock;

		private List<PipeNode> SavedPipes = new List<PipeNode>();

		private HashSet<GameObject> pushRecently = new HashSet<GameObject>();
		private int pushTimer;

		public bool DEBUGpointLock = false;
		public bool DEBUGPushBlock = false;
		public bool DEBUGDestroyBlock = false;
		public bool DEBUGMoveBlock = false;
		public bool DEBUGExplosionBlock = false;
		public bool DEBUGRadiationBlock = false;

		private readonly List<Vector3Int> adjacentCoords = new List<Vector3Int>
		{
			new Vector3Int(0, 1, 0),
			new Vector3Int(1, 0, 0),
			new Vector3Int(0, -1, 0),
			new Vector3Int(-1, 0, 0)
		};

		private readonly List<Tuple<int, int>> stagePointsBounds = new List<Tuple<int, int>>()
		{
			{ Tuple.Create(150, 199) }, // We spawn at 150 points; don't want to instantly scale (as it would be if as expected '0')
			{ Tuple.Create(200, 499) },
			{ Tuple.Create(500, 999) },
			{ Tuple.Create(1000, 1999) },
			{ Tuple.Create(2000, 2999) },
			{ Tuple.Create(3000, 3250) }
		};

		#region LifeCycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			ObjectPhysics = GetComponent<UniversalObjectPhysics>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			boxCollider2D = GetComponent<BoxCollider2D>();

			lightTransform = light.transform;
			objectId = GetInstanceID();

			WarpEffectFrontMat = WarpEffectFront.GetComponent<MeshRenderer>().materials[0];
			WarpEffectBackMat = WarpEffectBack.GetComponent<MeshRenderer>().materials[0];
		}

		private void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;

			CurrentStage = startingStage;
			currentFacing = currentFacing.Rotate(Random.Range(1, 4)); //Random direction on start
		}

		private void OnEnable()
		{
			UpdateManager.Add(SingularityUpdate, updateFrequency);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, SingularityUpdate);
		}

		//Sync light scale to clients
		[Client]
		private void SyncLightVector(Vector3 oldVar, Vector3 newVar)
		{
			lightVector = newVar;
			lightTransform.localScale = newVar;
		}

		private void SyncDynamicScale(Vector3 oldScale, Vector3 newScale)
		{
			if (newScale.Equals(Vector3.zero))
			{
				gameObject.transform.localScale = Vector3.one;
			}

			gameObject.transform.LeanScale(newScale, updateFrequency);

		}

		private void SyncCurrentStage(SingularityStages oldStage, SingularityStages newStage)
		{
			currentStage = newStage;
			UpdateWarpFX(currentStage);
		}

		#endregion

		/// <summary>
		/// Update loop, runs every 0.5 seconds
		/// </summary>
		private void SingularityUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			pushTimer++;

			if (pushTimer > 5)
			{
				pushTimer = 0;
				pushRecently.Clear();
			}

			if (lockTimer > 0)
			{
				lockTimer--;
			}

			if (lockTimer == 0)
			{
				pointLock = false;
			}

			if (singularityPoints <= 0 && zeroPointDeath)
			{
				Chat.AddActionMsgToChat(gameObject, "The singularity implodes!");
				RadiationManager.Instance.RequestPulse( registerTile.WorldPositionServer, maxRadiation, objectId);
				_ = Despawn.ServerSingle(gameObject);
				return;
			}

			//Points decrease by 4 every 0.5 seconds, unless locked by PA at setting 0
			if (pointLock == false && DEBUGpointLock == false)
			{
				ChangePoints(-pointLossRate);
			}

			TryChangeStage();

			PushPullObjects();

			DestroyObjectsAndTiles();

			TryMove();

			TryChangeStage();

			// will sync to clients
			dynamicScale = GetDynamicScale();

			ColliderScale();

			//Radiation Pulse
			var radStrength = Mathf.Max(((float) CurrentStage + 1) / 6 * maxRadiation, 0);

			if (DEBUGRadiationBlock == false)
			{
				RadiationManager.Instance.RequestPulse(registerTile.WorldPositionServer, radStrength, objectId);
			}


			if(DMMath.Prob(5) && DEBUGExplosionBlock == false)
			{
				int empStrength = Random.Range((int)CurrentStage * 100, (int)CurrentStage * 100 + 300);
				Vector3Int empPosition = registerTile.WorldPositionServer;
				empPosition.x += Random.Range(-3 - (int)CurrentStage, 3 + (int)CurrentStage);
				empPosition.y += Random.Range(-3 - (int)CurrentStage, 3 + (int)CurrentStage);
				Explosion.StartExplosion(empPosition, empStrength, new ExplosionEmpNode());
			}
		}

		#region Throw/Push

		private void PushPullObjects()
		{
			if (DEBUGPushBlock) return;
			int distance;

			switch (currentStage)
			{
				case SingularityStages.Stage0:
					distance = 1;
					break;
				case SingularityStages.Stage1:
					distance = 2;
					break;
				case SingularityStages.Stage2:
					distance = 3;
					break;
				case SingularityStages.Stage3:
					distance = 8;
					break;
				case SingularityStages.Stage4:
					distance = 10;
					break;
				case SingularityStages.Stage5:
					distance = 14;
					break;
				default:
					distance = 1;
					break;
			}

			foreach (var tile in EffectShape.CreateEffectShape(EffectShapeType.Circle, registerTile.WorldPositionServer, distance).ToList())
			{
				if (DMMath.Prob(50)) continue;

				var objects = MatrixManager.GetAt<UniversalObjectPhysics>(tile, true);

				foreach (var objectToMove in objects)
				{
					if (objectToMove.registerTile.ObjectType == ObjectType.Item)
					{
						if(pushRecently.Contains(objectToMove.gameObject)) continue;

						ThrowItem(objectToMove, registerTile.WorldPositionServer - objectToMove.registerTile.WorldPositionServer);

						if (objectToMove.TryGetComponent<Integrity>(out var integrity) && integrity != null)
						{
							//Just to stop never ending items being thrown about, and increasing points forever
							integrity.ApplyDamage(10, AttackType.Melee, DamageType.Brute, true, ignoreArmor: true);
						}
					}
					else
					{
						PushObject(objectToMove, registerTile.WorldPositionServer - objectToMove.registerTile.WorldPositionServer);
					}
				}
			}
		}

		private void ThrowItem(UniversalObjectPhysics item, Vector3 throwVector)
		{
			Vector3 vector = item.transform.rotation * throwVector;
			UniversalObjectPhysics itemTransform = item.GetComponent<UniversalObjectPhysics>();
			if (itemTransform == null) return;
			itemTransform.NewtonianPush(vector,1, 0,0,(BodyPartType) Random.Range(0, 13),  gameObject, 1 );
			pushRecently.Add(item.gameObject);
		}

		private void PushObject(UniversalObjectPhysics objectToPush, Vector3 pushVector)
		{
			if (CurrentStage == SingularityStages.Stage5 || CurrentStage == SingularityStages.Stage4)
			{
				//Try stun player
				if (DMMath.Prob(10) && TryGetComponent<PlayerHealthV2>(out var playerHealth) && playerHealth != null
				&& !playerHealth.IsDead)
				{
					playerHealth.RegisterPlayer.ServerStun();
					Chat.AddActionMsgToChat(objectToPush.gameObject, "You are knocked down by the singularity",
						$"{objectToPush.gameObject.ExpensiveName()} is knocked down by the singularity");
				}
			}
			else if (objectToPush.IsNotPushable)
			{
				//Dont push anchored objects unless stage 5 or 4
				return;
			}

			//Force Push Twice
			objectToPush.NewtonianNewtonPush(pushVector.NormalizeTo2Int(), 10);
		}

		#endregion

		#region Damage

		private void DestroyObjectsAndTiles()
		{
			if (DEBUGDestroyBlock) return;

			int radius;

			switch (currentStage)
			{
				case SingularityStages.Stage0:
					radius = 0;
					break;
				case SingularityStages.Stage1:
					radius = 1;
					break;
				case SingularityStages.Stage2:
					radius = 2;
					break;
				case SingularityStages.Stage3:
					radius = 3;
					break;
				case SingularityStages.Stage4:
					radius = 4;
					break;
				case SingularityStages.Stage5:
					radius = 5;
					break;
				default:
					radius = 0;
					break;
			}

			var coords =
				EffectShape.CreateEffectShape(EffectShapeType.Square, registerTile.WorldPositionServer, radius);

			DestroyObjects(coords);
			DamageTiles(coords);
		}

		private void DestroyObjects(IEnumerable<Vector3Int> coords)
		{
			var damage = 100;

			if (CurrentStage == SingularityStages.Stage5 || CurrentStage == SingularityStages.Stage4)
			{
				damage *= (int)CurrentStage;
			}

			foreach (var coord in coords)
			{
				var objects = MatrixManager.GetAt<RegisterTile>(coord, true);

				foreach (var objectToMove in objects)
				{
					if(objectToMove.gameObject == gameObject) continue;

					//Check for player
					if (objectToMove.ObjectType == ObjectType.Player && objectToMove.TryGetComponent<PlayerHealthV2>(out var health) && health != null)
					{
						if (health.RegisterPlayer.PlayerScript.Mind?.occupation == OccupationList.Instance.Get(JobType.CLOWN))
						{
							health.OnGib();
							ChangePoints(DMMath.Prob(50) ? -1000 : 1000);
							return;
						}

						health.OnGib();
						ChangePoints(100);
						return;
					}

					//Check for objects
					if (objectToMove.TryGetComponent<Integrity>(out var integrity) && integrity != null)
					{
						//Check for field gens and only damage at high level
						if (objectToMove.TryGetComponent<FieldGenerator>(out var fieldGenerator)
						    && fieldGenerator != null)
						{
							if (CurrentStage != SingularityStages.Stage4 && CurrentStage != SingularityStages.Stage5 &&
							    fieldGenerator.Energy != 0)
							{
								//Stages below 4 can only damage field generators if they have no energy
								return;
							}
						}

						//See if it's a supermatter, uh oh....
						if (objectToMove.TryGetComponent<SuperMatter>(out var superMatter) && superMatter != null)
						{
							//End of the world
							eatenSuperMatter = true;
							ChangePoints(3250);
							_ = Despawn.ServerSingle(objectToMove.gameObject);
							Chat.AddActionMsgToChat(gameObject, "<color=red>The singularity expands rapidly, uh oh...</color>");
							return;
						}

						integrity.ApplyDamage(damage, AttackType.Melee, DamageType.Brute, true);
						ChangePoints(5);
					}
				}
			}
		}

		private void DamageTiles(IEnumerable<Vector3Int> coords)
		{
			foreach (var coord in coords)
			{
				var matrixInfo = MatrixManager.AtPoint(coord, true);

				var cellPos = MatrixManager.WorldToLocalInt(coord, matrixInfo.Matrix);

				var layerTile = matrixInfo.TileChangeManager.MetaTileMap
					.GetTile(MatrixManager.WorldToLocalInt(coord, matrixInfo), LayerType.Walls);

				if (CurrentStage != SingularityStages.Stage5 && CurrentStage != SingularityStages.Stage4 &&
					layerTile != null && (layerTile.name == horizontal.name || layerTile.name == vertical.name)) continue;

				matrixInfo.Matrix.MetaTileMap.ApplyDamage(
					cellPos,
					200,
					coord,
					AttackType.Bomb);

				var node = matrixInfo.Matrix.GetMetaDataNode(cellPos);
				if (node != null)
				{
					foreach (var electricalData in node.ElectricalData)
					{
						electricalData.InData.DestroyThisPlease();
						ChangePoints(5);
					}

					SavedPipes.Clear();
					SavedPipes.AddRange(node.PipeData);
					foreach (var pipe in SavedPipes)
					{
						pipe.pipeData.DestroyThis();
						ChangePoints(5);
					}
				}
			}
		}

		#endregion

		#region Movement

		private void TryMove()
		{
			if (DEBUGMoveBlock) return;
			int radius = GetRadius(CurrentStage);

			var coord = currentFacing.LocalVectorInt.To3Int() + registerTile.WorldPositionServer;

			bool noObstructions = true;

			//See whether we can move to that place, as we need to take into account our size
			if (CurrentStage != SingularityStages.Stage5 && CurrentStage != SingularityStages.Stage4)
			{
				foreach (var squareCoord in EffectShape.CreateEffectShape(EffectShapeType.OutlineSquare, coord, radius))
				{
					if (MatrixManager.IsPassableAtAllMatricesOneTile(squareCoord, true, false, new List<LayerType>{LayerType.Objects}) == false)
					{
						noObstructions = false;
						break;
					}
				}
			}

			//If could not fit, damage coords around ourself if stage big enough
			if (noObstructions == false)
			{
				if (CurrentStage != SingularityStages.Stage5 && CurrentStage != SingularityStages.Stage4)
				{
					//Hit in front, to give a bump effect so smaller singularity doesnt get stuck in walls
					HitLineInFront(radius, currentFacing.LocalVectorInt.To3Int());
					return;
				}

				var squareCoord = EffectShape.CreateEffectShape(EffectShapeType.OutlineSquare,
					registerTile.WorldPositionServer, radius + 1, false);

				DestroyObjects(squareCoord);
				DamageTiles(squareCoord);
				return;
			}

			if (Random.Range(0, 101) >= directionThreshold) currentFacing = currentFacing.Rotate(Random.Range(1, 4)); //Random new angle excluding current angle.
			//Move
			ObjectPhysics.AppearAtWorldPositionServer(coord, true);
		}

		/// <summary>
		/// This method hits a line of coords at radius + 1, effectively causes the singularity to bump into a blocked
		/// direction doing damage if possible
		/// </summary>
		/// <param name="radius"></param>
		/// <param name="adjacentCoord"></param>
		private void HitLineInFront(int radius, Vector3Int adjacentCoord)
		{
			var hitCoords = new List<Vector3Int>();

			var baseCoord = Vector3Int.right * (radius + 1);

			hitCoords.Add(baseCoord);

			for (int i = 1; i <= radius; i++)
			{
				var newCoord = baseCoord;
				newCoord.y += i;
				hitCoords.Add(newCoord);
				newCoord.y -= i * 2;
				hitCoords.Add(newCoord);
			}

			var orientation = Orientation.Right.RotationsTo(Orientation.From(adjacentCoord.To2Int()));

			if (orientation == -1)
			{
				orientation = 3;
			}

			var damageCoords = new List<Vector3Int>();

			foreach (var hit in hitCoords)
			{
				var rotatedVector = hit;
				for (int i = 1; i <= orientation; i++)
				{
					rotatedVector = RotateVector90(rotatedVector.To2Int());
				}

				damageCoords.Add(rotatedVector + registerTile.WorldPositionServer);
			}

			DestroyObjects(damageCoords);
			DamageTiles(damageCoords);
		}

		#endregion

		#region ChangeStage

		private void TryChangeStage()
		{
			if (singularityPoints < 200)
			{
				ChangeStage(SingularityStages.Stage0);
			}
			else if (singularityPoints < 500)
			{
				ChangeStage(SingularityStages.Stage1);
			}
			else if (singularityPoints < 1000)
			{
				ChangeStage(SingularityStages.Stage2);
			}
			else if (singularityPoints < 2000)
			{
				ChangeStage(SingularityStages.Stage3);
			}
			else if ((singularityPoints >= 2000 && !eatenSuperMatter) || (singularityPoints >= 3000 && !eatenSuperMatter))
			{
				ChangeStage(SingularityStages.Stage4);
			}
			else if (singularityPoints >= 3000 && eatenSuperMatter)
			{
				ChangeStage(SingularityStages.Stage5);
			}
		}

		private void ChangeStage(SingularityStages newStage)
		{
			if (CurrentStage == newStage) return;

			if(preventDowngrade && newStage < CurrentStage) return;

			int radius = GetRadius(newStage);

			bool noObstructions = true;

			//See whether we can move to that place, as we need to take into account our size
			if (newStage != SingularityStages.Stage5 && newStage != SingularityStages.Stage4)
			{
				foreach (var squareCoord in EffectShape.CreateEffectShape(EffectShapeType.OutlineSquare, registerTile.WorldPositionServer, radius))
				{
					if (MatrixManager.IsPassableAtAllMatricesOneTile(squareCoord, true, false) == false)
					{
						noObstructions = false;
						break;
					}
				}
			}

			if (noObstructions)
			{
				Chat.AddActionMsgToChat(gameObject, $"The singularity fluctuates and {(newStage < CurrentStage ? "decreases" : "increases")} in size!");
				CurrentStage = newStage;
				UpdateVectors();
				dynamicScale = Vector3.zero; // keyed value: don't tween; set it to 1x scale immediately
			}
		}
		/// <summary>
		/// Sets the warp effects in accordance with the correct sprite
		/// </summary>
		private void UpdateWarpFX(SingularityStages stage)
		{
			float scaledRadius = 0f;
			float scaledEffect = 0f;

			switch (stage)
			{
				case SingularityStages.Stage0:
					scaledRadius = Mathf.Clamp(0.08f * gameObject.transform.localScale.x, 0.08f,0.15f);
					scaledEffect = 7f;
					break;
				case SingularityStages.Stage1:
					scaledRadius = Mathf.Clamp(0.26f * gameObject.transform.localScale.x, 0.26f,0.35f);
					scaledEffect = 9f;
					break;
				case SingularityStages.Stage2:
					scaledRadius = Mathf.Clamp(0.35f * gameObject.transform.localScale.x, 0.35f, 0.6f);
					scaledEffect = 10f;
					break;
				case SingularityStages.Stage3:
					scaledRadius = Mathf.Clamp(0.55f * gameObject.transform.localScale.x, 0.55f, 0.75f);
					scaledEffect = 12f;
					break;
				case SingularityStages.Stage4:
					scaledRadius = Mathf.Clamp(0.75f * gameObject.transform.localScale.x, 0.75f, 0.8f);
					scaledEffect = 15f;
					break;
				case SingularityStages.Stage5:
					scaledRadius = Mathf.Clamp(0.8f * gameObject.transform.localScale.x, 0.8f, 1f);
					scaledEffect = 20f;
					break;
				default:
					break;
			}

			WarpEffectFrontMat.SetFloat("_EffectRadius", scaledRadius);
			WarpEffectBackMat.SetFloat("_EffectRadius", scaledRadius);
			WarpEffectFrontMat.SetFloat("_EffectAngle", scaledEffect);
			WarpEffectBackMat.SetFloat("_EffectAngle", scaledEffect);
		}

		private void UpdateVectors()
		{
			int stage = (int)CurrentStage + 1;
			lightVector = new Vector3(5 * stage, 5 * stage, 0);
		}

		#endregion

		#region Points

		private void ChangePoints(int healthChange)
		{
			if (singularityPoints + healthChange >= maximumPoints)
			{
				singularityPoints = maximumPoints;
				return;
			}

			if (singularityPoints + healthChange < maximumPoints)
			{
				singularityPoints += healthChange;
				return;
			}

			if (singularityPoints < 0)
			{
				singularityPoints = 0;
			}
		}

		public void OnHitDetect(OnHitDetectData data)
		{
			if(data.DamageData.AttackType != AttackType.Rad) return;

			if (data.DamageData.Damage >= 19f)
			{
				// PA at any setting will prevent point loss
				pointLock = true;
				lockTimer = 20;
			}

			if (data.DamageData.Damage > 21f)
			{
				// PA at setting greater than 0 will do 20 damage
				ChangePoints((int)data.DamageData.Damage);
			}
		}

		#endregion

		#region Collider

		private void ColliderScale()
		{
			var size = 1;

			if (currentStage != SingularityStages.Stage0)
			{
				size = 3;
			}

			var scale = dynamicScale.x * size;
			boxCollider2D.size = new Vector2(scale, scale);
		}

		#endregion

		/// <summary>
		/// Gets dynamic scaling size for singularity.
		/// Linear between min size -> no extra scaling and max stage size -> 3x scaling,
		/// for smooth transitions between states.
		/// </summary>
		private Vector3 GetDynamicScale()
		{
			int stageNum = (int)CurrentStage;

			// possible to go below 1 on stage 0 as we define min scale for this stage as 150 points, not 0 (we spawn with 150)
			int currentPoints = Math.Max(singularityPoints, stagePointsBounds[stageNum].Item1);
			int stageMax = stagePointsBounds[stageNum].Item2;

			float scale = (currentPoints / (float)stageMax);

			// possible to be scaled further than the size of the next stage -> points exceeds next stage but can't grow (obstructions)
			scale = Math.Min(scale, 1);

			return new Vector3(scale, scale, 1);
		}

		/// <summary>
		/// Gets size radius for movement and stage change
		/// </summary>
		private int GetRadius(SingularityStages stage)
		{
			switch (stage)
			{
				case SingularityStages.Stage0:
					return 0;
				case SingularityStages.Stage1:
					return 1;
				case SingularityStages.Stage2:
					return 2;
				case SingularityStages.Stage3:
					return 3;
				case SingularityStages.Stage4:
					return 3;
				case SingularityStages.Stage5:
					return 3;
				default:
					return 0;
			}
		}

		//Rotate vector 90 degrees ANTI-clockwise
		private Vector3Int RotateVector90(Vector2Int vector)
		{
			//Woo for easy math
			return new Vector3Int(-vector.y, vector.x, 0);
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return pointLock ? "Singularity looks stable" : "Singularity looks unstable";
		}
	}

	public enum SingularityStages
	{
		Stage0 = 0,
		Stage1 = 1,
		Stage2 = 2,
		Stage3 = 3,
		Stage4 = 4,
		Stage5 = 5
	}
}
