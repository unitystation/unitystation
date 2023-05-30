using System;
using UnityEngine;
using NaughtyAttributes;
using AddressableReferences;
using Messages.Server;
using Systems.Clearance;
using Systems.Electricity;
using Systems.Electricity.NodeModules;
using Systems.Interaction;
using Weapons.Projectiles;


namespace Objects.Engineering
{
	public class Emitter : MonoBehaviour, ICheckedInteractable<HandApply>, INodeControl, IExaminable, ICheckedInteractable<AiActivate>
	{
		private Rotatable directional;
		private UniversalObjectPhysics objectBehaviour;
		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private AccessRestrictions accessRestrictions;
		private ClearanceRestricted clearanceRestricted;
		private ElectricalNodeControl electricalNodeControl;

		[SerializeField]
		private GameObject projectilePrefab = default;

		[SerializeField]
		[Tooltip("Whether this emitter should start wrenched and welded")]
		private bool startSetUp;

		[SerializeField, ShowIf(nameof(startSetUp))]
		[Tooltip("Whether or not it should try shooting straight away")]
		private bool startOn;

		[SerializeField]
		[Tooltip("Whether this emitter should always shoot even if no power")]
		private bool alwaysShoot;

		[SerializeField]
		[Tooltip("The minimum voltage necessary to shoot")]
		private float minVoltage = 1500f;

		[SerializeField]
		[Tooltip("Sound made when emitter shoots")]
		[Foldout("AddressableSound")]
		private AddressableAudioSource sound = null;

		[SerializeField]
		[Tooltip("Used if you want Overwrite functionality and prevent it from firing")]
		private bool DoesNotShoot = false;


		private bool isWelded;
		private bool isWrenched;
		private bool isOn;

		public bool IsOn => isOn;

		private bool isLocked;

		// Voltage in wire
		private float voltage;

		#region LifeCycle

		private void Awake()
		{
			directional = GetComponent<Rotatable>();
			objectBehaviour = GetComponent<UniversalObjectPhysics>();
			registerTile = GetComponent<RegisterTile>();
			accessRestrictions = GetComponent<AccessRestrictions>();
			clearanceRestricted = GetComponent<ClearanceRestricted>();
			electricalNodeControl = GetComponent<ElectricalNodeControl>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
		}

		private void Start()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if (startSetUp)
			{
				isWelded = true;
				isWrenched = true;
				directional.LockDirectionTo(true, directional.CurrentDirection);
				objectBehaviour.SetIsNotPushable(true);
				TogglePower(startOn);
			}
		}

		private void OnEnable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Add(EmitterUpdate, 1f);
		}

		private void OnDisable()
		{
			if(CustomNetworkManager.IsServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, EmitterUpdate);
		}

		#endregion

		/// <summary>
		/// Update Loop, runs every 1 second
		/// Server Side Only
		/// </summary>
		private void EmitterUpdate()
		{

			if (ValidSetup() == false) return;
			if (DoesNotShoot) return;

			//Shoot 75% of the time, to add variation
			if(DMMath.Prob(25)) return;

			ShootEmitter();
		}


		public bool ValidSetup(bool IgnoreOn = false)
		{
			if (isWrenched == false) return false;
			if (isWelded == false) return false;
			if (IgnoreOn == false)
			{
				if(isOn == false && alwaysShoot == false) return false;
			}


			if (voltage < minVoltage && alwaysShoot == false)
			{
				spriteHandler.ChangeSprite(2);
				return false;
			}

			//Reset sprite if power is now available
			TogglePower(isOn);
			return true;
		}

		public void ShootEmitter()
		{
			ProjectileManager.InstantiateAndShoot( projectilePrefab, directional.WorldDirection,gameObject, default);

			SoundManager.PlayNetworkedAtPos(sound, registerTile.WorldPositionServer);
		}

		public void PowerNetworkUpdate()
		{
			voltage = electricalNodeControl.GetVoltage();
		}

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Welder)) return true;

			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id)) return true;

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
			else if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Id))
			{
				TryToggleLock(interaction);
			}
			else
			{
				TryToggleOnOff(interaction);
			}
		}

		#endregion

		#region Lock

		private void TryToggleLock(HandApply interaction)
		{
			if (clearanceRestricted.HasClearance(interaction.Performer))
			{
				ToggleEmitter();
			}

			void ToggleEmitter()
			{
				isLocked = !isLocked;

				Chat.AddActionMsgToChat(interaction.Performer,
					$"You {(isLocked ? "lock" : "unlock" )} the emitter",
					$"{interaction.Performer.ExpensiveName()} {(isLocked ? "locks" : "unlocks" )} the emitter");
			}
		}

		#endregion

		#region Power

		private void TryToggleOnOff(HandApply interaction)
		{
			if (isLocked)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The emitter needs to be unlocked first");
				return;
			}

			if (isOn)
			{
				Chat.AddActionMsgToChat(interaction.Performer, "You turn the emitter off",
					$"{interaction.Performer.ExpensiveName()} turns the emitter off");

				TogglePower(false);
			}
			else if (isWelded)
			{
				Chat.AddActionMsgToChat(interaction.Performer, "You turn the emitter on",
					$"{interaction.Performer.ExpensiveName()} turns the emitter on");

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
				spriteHandler.ChangeSprite(1);
			}
			else
			{
				isOn = false;
				spriteHandler.ChangeSprite(0);
			}
		}

		#endregion

		#region Weld

		private void TryWeld(HandApply interaction)
		{
			if (isOn)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be turned off first");
				return;
			}

			if (isWrenched == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "Emitter needs to be wrenched down first");
				return;
			}

			if (interaction.HandObject.GetComponent<Welder>().IsOn == false)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You need a fueled and lit welder");
				return;
			}

			if (isWelded)
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					"You start to unweld the emitter...",
					$"{interaction.Performer.ExpensiveName()} starts to unweld the emitter...",
					"You unweld the emitter from the floor.",
					$"{interaction.Performer.ExpensiveName()} unwelds the emitter from the floor.",
					() =>
					{
						ElectricalManager.Instance.electricalSync.StructureChange = true;
						isWelded = false;
						TogglePower(false);
					});
			}
			else
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 3,
					"You start to weld the emitter...",
					$"{interaction.Performer.ExpensiveName()} starts to weld the emitter...",
					"You weld the emitter to the floor.",
					$"{interaction.Performer.ExpensiveName()} welds the emitter to the floor.",
					() =>
					{
						ElectricalManager.Instance.electricalSync.StructureChange = true;
						isWelded = true;
					});
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
						directional.LockDirectionTo(false, directional.CurrentDirection);
						objectBehaviour.SetIsNotPushable(false);
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
						directional.LockDirectionTo(true, directional.CurrentDirection);
						objectBehaviour.SetIsNotPushable(true);
					});
			}
		}

		#endregion

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return $"Status: {isOn} and {isLocked}{(voltage < minVoltage && !alwaysShoot? $", voltage needs to be {minVoltage} to fire" : "")}";
		}

		#region Ai Interaction

		public bool WillInteract(AiActivate interaction, NetworkSide side)
		{
			if (DefaultWillInteract.AiActivate(interaction, side) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(AiActivate interaction)
		{
			if (isLocked)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The emitter has been locked");
				return;
			}

			if (isOn)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You turn the emitter off");

				TogglePower(false);
			}
			else if (isWelded)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "You turn the emitter on");

				TogglePower(true);
			}
			else
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The emitter has not been set up");
			}
		}

		#endregion
	}
}
