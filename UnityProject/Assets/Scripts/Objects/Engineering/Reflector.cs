using System;
using Messages.Server;
using Mirror;
using ScriptableObjects;
using ScriptableObjects.Gun;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Weapons;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

namespace Objects.Engineering
{
	public class Reflector : NetworkBehaviour, IOnHitDetect, ICheckedInteractable<HandApply>
	{
		[SerializeField] private ReflectorType startingState = ReflectorType.Base;
		private ReflectorType currentState = ReflectorType.Base;

		[SerializeField, Range(0f, 360f)] private float startingAngle = 0;

		[SerializeField] private bool startSetUp;

		[SerializeField] private Transform spriteTransform;

		private SpriteHandler spriteHandler;
		private UniversalObjectPhysics objectBehaviour;
		private RegisterTile registerTile;
		private ObjectAttributes objectAttributes;
		private Integrity integrity;

		public event Action AngleChange;

		[SerializeField] private int glassNeeded = 5;
		[SerializeField] private int reinforcedGlassNeeded = 10;
		[SerializeField] private int diamondsNeeded = 1;

		[SerializeField]
		//Use to check whether a bullet is a laser
		private LayerMaskData laserData = null;

		private bool isWelded;

		[SyncVar(hook = nameof(SyncRotation))] private float rotation;

		#region LifeCycle

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			registerTile = GetComponent<RegisterTile>();
			objectAttributes = GetComponent<ObjectAttributes>();
			integrity = GetComponent<Integrity>();
		}

		private void OnValidate()
		{
			if (Application.isPlaying || this == null) return;
#if UNITY_EDITOR
			EditorApplication.delayCall -= ValidateLate;
			EditorApplication.delayCall += ValidateLate;
#endif

		}

		public void ValidateLate()
		{
			if (Application.isPlaying || this == null) return;
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			currentState = startingState;
			spriteHandler.ChangeSprite((int) startingState);
			SyncRotation(rotation, startingAngle);
			transform.localEulerAngles = new Vector3(0, 0, rotation);
			spriteTransform.localEulerAngles = Vector3.zero;
		}


		private void Start()
		{
			if (CustomNetworkManager.IsServer == false) return;

			ChangeState(startingState);
			SyncRotation(rotation, startingAngle);
			transform.localEulerAngles = new Vector3(0, 0, rotation);
			spriteTransform.localEulerAngles = Vector3.zero;

			if (startSetUp)
			{
				isWelded = true;
				objectBehaviour.SetIsNotPushable(true);
			}
		}

		private void SyncRotation(float oldVar, float newVar)
		{
			rotation = newVar;
			transform.localEulerAngles = new Vector3(0, 0, rotation);
			spriteTransform.localEulerAngles = Vector3.zero;
			AngleChange?.Invoke();
		}

		private void OnEnable()
		{
			integrity.OnWillDestroyServer.AddListener(OnDestruction);
		}

		private void OnDisable()
		{
			integrity.OnWillDestroyServer.RemoveListener(OnDestruction);
		}

		private void OnDestroy()
		{
			AngleChange = null;
		}

		#endregion

		private void ChangeState(ReflectorType newState)
		{
			currentState = newState;
			spriteHandler.ChangeSprite((int) newState);
			objectAttributes.ServerSetArticleName(newState + " Reflector");
		}

		private void OnDestruction(DestructionInfo info)
		{
			DownGradeState();
		}

		#region HandApply

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.ReinforcedGlassSheet))
				return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.DiamondSheet)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder))
			{
				TryWeld(interaction);
				return;
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				TryWrench(interaction);
				return;
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Screwdriver))
			{
				TryRotate(interaction);
				return;
			}

			TryBuild(interaction);
		}

		private void TryWeld(HandApply interaction)
		{
			if (Validations.HasUsedActiveWelder(interaction) == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The welder needs to be turn on first");
				return;
			}

			if (isWelded)
			{
				//Unweld
				ToolUtils.ServerUseToolWithActionMessages(interaction, 5,
					$"You begin to unweld the {gameObject.ExpensiveName()}...",
					$"{interaction.Performer.ExpensiveName()} starts unweld the {gameObject.ExpensiveName()} from the floor...",
					$"You unweld the {gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} unwelds the {gameObject.ExpensiveName()}",
					() =>
					{
						isWelded = false;
						objectBehaviour.SetIsNotPushable(false);
					}
				);

				return;
			}

			if (currentState != ReflectorType.Base)
			{
				//Weld to floor if not base
				ToolUtils.ServerUseToolWithActionMessages(interaction, 5,
					$"You begin to weld the {gameObject.ExpensiveName()}...",
					$"{interaction.Performer.ExpensiveName()} starts weld the {gameObject.ExpensiveName()} to the floor...",
					$"You weld the {gameObject.ExpensiveName()}",
					$"{interaction.Performer.ExpensiveName()} welds the {gameObject.ExpensiveName()}",
					() =>
					{
						isWelded = true;
						objectBehaviour.SetIsNotPushable(true);
					}
				);

				return;
			}

			//Needs to be fully constructed before weld
			Chat.AddExamineMsgFromServer(interaction.Performer, "The reflector needs to be constructed first");
		}

		//Deconstruction
		private void TryWrench(HandApply interaction)
		{
			if (isWelded)
			{
				//Needs to be unwelded before deconstruction
				Chat.AddExamineMsgFromServer(interaction.Performer, "The reflector needs to be unwelded first");
				return;
			}

			ToolUtils.ServerUseToolWithActionMessages(interaction, 5,
				$"You begin to deconstuct the {gameObject.ExpensiveName()} with the {interaction.HandObject.ExpensiveName()}...",
				$"{interaction.Performer.ExpensiveName()} starts deconstructing the {gameObject.ExpensiveName()}...",
				$"You deconstruct the {gameObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()} deconstructs the {gameObject.ExpensiveName()}",
				() => { DownGradeState(); }
			);
		}

		private void TryRotate(HandApply interaction)
		{
			if (isWelded == false)
			{
				//Needs to be unwelded before deconstruction
				Chat.AddExamineMsgFromServer(interaction.Performer, "The reflector needs to be unwelded first");
				return;
			}

			if (currentState == ReflectorType.Base)
			{
				//Needs to be constructed first
				Chat.AddExamineMsgFromServer(interaction.Performer, "The reflector needs to be constructed first");
				return;
			}


			float NewRotate = 0;
			if (interaction.IsAltClick)
			{
				NewRotate = rotation + 5;
			}
			else
			{
				NewRotate = rotation - 5;
			}

			if (NewRotate >= 360)
			{
				NewRotate -= 360;
			}
			else if (NewRotate < 0)
			{
				rotation += 360;
			}

			SyncRotation(rotation, NewRotate);
			Chat.AddExamineMsgFromServer(interaction.Performer, $"You rotate the reflector to {rotation - 90} degrees");
		}

		private void DownGradeState()
		{
			switch (currentState)
			{
				case ReflectorType.Base:
					SpawnMaterial(CommonPrefabs.Instance.Metal, 4);
					_ = Despawn.ServerSingle(gameObject);
					return;
				case ReflectorType.Box:
					SpawnMaterial(CommonPrefabs.Instance.DiamondSheet, diamondsNeeded - 1);
					ChangeState(ReflectorType.Base);
					break;
				case ReflectorType.Double:
					SpawnMaterial(CommonPrefabs.Instance.ReinforcedGlassSheet, reinforcedGlassNeeded - 1);
					ChangeState(ReflectorType.Base);
					break;
				case ReflectorType.Single:
					SpawnMaterial(CommonPrefabs.Instance.GlassSheet, glassNeeded - 1);
					ChangeState(ReflectorType.Base);
					break;
			}
		}

		private void SpawnMaterial(GameObject prefab, int amountToIncrease = 0)
		{
			var resultBase = Spawn.ServerPrefab(prefab, registerTile.WorldPositionServer, transform.parent);

			if (resultBase.Successful == false || amountToIncrease == 0) return;

			resultBase.GameObject.GetComponent<Stackable>().ServerIncrease(amountToIncrease);
		}

		private void TryBuild(HandApply interaction)
		{
			if (currentState != ReflectorType.Base) return;

			if (TryAddParts(interaction))
			{
				CompleteBuild();
			}
		}

		private bool TryAddParts(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.GlassSheet))
			{
				if (interaction.HandObject.TryGetComponent<Stackable>(out var stackable) &&
				    stackable.Amount >= glassNeeded)
				{
					stackable.ServerConsume(glassNeeded);
					currentState = ReflectorType.Single;

					Chat.AddActionMsgToChat(interaction.Performer,
						$"You add {glassNeeded} glass sheets to the reflector.",
						$"{interaction.Performer.ExpensiveName()} adds {glassNeeded} glass sheets to the reflector.");

					return true;
				}

				Chat.AddExamineMsgFromServer(interaction.Performer,
					$"You need {glassNeeded} glass sheets to build a single reflector.");
				return false;
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.ReinforcedGlassSheet))
			{
				if (interaction.HandObject.TryGetComponent<Stackable>(out var stackable) &&
				    stackable.Amount >= reinforcedGlassNeeded)
				{
					stackable.ServerConsume(reinforcedGlassNeeded);
					currentState = ReflectorType.Double;

					Chat.AddActionMsgToChat(interaction.Performer,
						$"You add {reinforcedGlassNeeded} reinforced glass sheets to the reflector.",
						$"{interaction.Performer.ExpensiveName()} adds {reinforcedGlassNeeded} reinforced glass sheets to the reflector.");
					return true;
				}

				Chat.AddExamineMsgFromServer(interaction.Performer,
					$"You need {reinforcedGlassNeeded} reinforced glass sheets to build a double reflector.");
				return false;
			}

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.DiamondSheet))
			{
				if (interaction.HandObject.TryGetComponent<Stackable>(out var stackable) &&
				    stackable.Amount >= diamondsNeeded)
				{
					stackable.ServerConsume(diamondsNeeded);
					currentState = ReflectorType.Box;

					Chat.AddActionMsgToChat(interaction.Performer,
						$"You add {diamondsNeeded} glass sheets to the reflector.",
						$"{interaction.Performer.ExpensiveName()} adds {diamondsNeeded} diamond to the reflector.");
					return true;
				}

				Chat.AddExamineMsgFromServer(interaction.Performer,
					$"You need {diamondsNeeded} diamond sheets to build a box reflector.");
				return false;
			}

			return false;
		}

		private void CompleteBuild()
		{
			ChangeState(currentState);
		}

		#endregion

		private enum ReflectorType
		{
			Base,
			Box,
			Double,
			Single
		}

		public bool ValidState()
		{
			if (currentState == ReflectorType.Base) return false;

			if (isWelded == false) return false;
			return true;
		}

		public float GetReflect(Vector2 InDirection)
		{
			switch (currentState)
			{
				//Sends all to rotation direction
				case ReflectorType.Box:
					return ReturnBox(InDirection);
					break;
				case ReflectorType.Double:
					return ReturnTryAngleDouble(InDirection);
					break;
				case ReflectorType.Single:
					return ReturnTryAngleSingle(InDirection);
					break;
			}

			return float.NaN;
		}

		public void OnHitDetect(OnHitDetectData data)
		{
			//Only reflect lasers
			if (data.BulletObject.TryGetComponent<Bullet>(out var bullet) == false ||
			    bullet.MaskData != laserData) return;

			if (ValidState() == false) return;

			float Angle = GetReflect(data.BulletShootDirection);
			if (float.IsNaN(Angle)) return;
			ShootAtDirection(Angle, data);
		}


		private float ConvertToWorldRotation(float Local)
		{
			if (Local >= 360)
			{
				Local -= 360;
			}
			else if (Local < 0)
			{
				Local += 360;
			}

			var ModifiedAngle = Local;

			if (registerTile.Matrix.MatrixMove != null)
			{
				ModifiedAngle = registerTile.Matrix.MatrixMove.CurrentState.FacingDirection.AsEnum().Rotate360By(ModifiedAngle);
			}

			// If the final angle is greater than or equal to 360 or less than 0, wrap it around.
			if (ModifiedAngle >= 360)
			{
				ModifiedAngle -= 360;
			}
			else if (ModifiedAngle < 0)
			{
				ModifiedAngle += 360;
			}
			return ModifiedAngle;
		}

		public float ReturnBox(Vector2 InDirection)
		{
			return ConvertToWorldRotation(rotation) + 90;
		}



		public float ReturnTryAngleSingle(Vector2 InDirection)
		{
			var incoming = InDirection.VectorToAngle360();

			var WorldRotation = ConvertToWorldRotation(rotation - 90);

			if (Vector2.Angle(InDirection,  VectorExtensions.DegreeToVector2(WorldRotation)) <= 85f)
			{
				float reflectedAngle = (2 * ConvertToWorldRotation(rotation)) - incoming;
				return reflectedAngle;
			}

			// if (Vector2.Angle(InDirection, VectorExtensions.DegreeToVector2(rotation - 90)) <= 55)
			// {
				// return rotation + 90;
			// }
			return float.NaN;
		}

		public float ReturnTryAngleDouble(Vector2 InDirection)
		{

			var incoming = InDirection.VectorToAngle360();

			var WorldRotation = ConvertToWorldRotation(rotation - 90);

			if (Vector2.Angle(InDirection,  VectorExtensions.DegreeToVector2(WorldRotation)) <= 85f)
			{
				float reflectedAngle = (2 * ConvertToWorldRotation(rotation)) - incoming;
				return reflectedAngle;
			}

			WorldRotation = ConvertToWorldRotation(rotation + 180 - 90);

			if (Vector2.Angle(InDirection,  VectorExtensions.DegreeToVector2(WorldRotation)) <= 85f)
			{
				float reflectedAngle = (2 * ConvertToWorldRotation(rotation + 180)) - incoming;
				return reflectedAngle;
			}

			// if (Vector2.Angle(InDirection, VectorExtensions.DegreeToVector2(rotation - 90)) <= 55)
			// {
				// return rotation + 90;
			// }
			// else if (Vector2.Angle(InDirection, VectorExtensions.DegreeToVector2(rotation + 180 - 90)) <=
			         // 55)
			// {
				// return rotation + 180 + 90;
			// }
			return float.NaN;
		}


		private void ShootAtDirection(float rotationToShoot, OnHitDetectData data)
		{
			var range = -1f;

			if (data.BulletObject.TryGetComponent<ProjectileRangeLimited>(out var rangeLimited))
			{
				range = rangeLimited.CurrentDistance;
			}

			ProjectileManager.CloneAndShoot(data, data.BulletObject.GetComponent<Bullet>().PrefabName,
				VectorExtensions.DegreeToVector2(rotationToShoot), gameObject, null, BodyPartType.None, range, data.HitWorldPosition);
		}
	}
}