using System;
using System.Collections.Generic;
using System.Text;
using Messages.Server;
using ScriptableObjects.Gun;
using UnityEngine;
using Systems.Electricity.NodeModules;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

namespace Objects.Engineering
{
	[RequireComponent(typeof(ParticleAcceleratorPart))]
	public class ParticleAcceleratorControl : MonoBehaviour, INodeControl, ICheckedInteractable<HandApply>
	{
		private ParticleAcceleratorPart selfPart;
		private List<ParticleAcceleratorPart> connectedParts = new List<ParticleAcceleratorPart>();

		private ElectricalNodeControl electricalNodeControl;
		private RegisterTile registerTile;
		private Orientation orientation = Orientation.Right;
		private bool connected;
		private float voltage;
		private bool isOn;

		[SerializeField]
		[Tooltip("How much power each next level will need, eg level three will need 800, 4 * value")]
		private float voltageIncreasePerPowerLevel = 200;

		[SerializeField]
		private GameObject particleAcceleratorBulletPrefab = null;

		private DamageData damageData = null;

		[SerializeField]
		[Tooltip("Whether to ignore voltage requirements")]
		private bool isAlwaysOn;

		[SerializeField]
		private float timeToHack = 20f;

		[SerializeField]
		private float chanceToFailHack = 25f;

		[SerializeField]
		private bool isHacked;
		public bool IsHacked => isHacked;

		private string status = "";
		public string Status => status;

		private string powerUsage = "0";
		public string PowerUsage => powerUsage;

		private ParticleAcceleratorState currentState = ParticleAcceleratorState.Off;
		public ParticleAcceleratorState CurrentState => currentState;

		//Based on Particle Accelerator pointing right, these vectors will be rotated 90 degrees anti-clockwise to find other directions
		private Dictionary<Vector2Int, ParticleAcceleratorType> machineBluePrint = new Dictionary<Vector2Int, ParticleAcceleratorType>()
		{
			{new Vector2Int(0, 1), ParticleAcceleratorType.FuelBox},
			{new Vector2Int(-1, 1), ParticleAcceleratorType.Back},
			{new Vector2Int(1, 1), ParticleAcceleratorType.PowerBox},
			{new Vector2Int(2, 1), ParticleAcceleratorType.FrontMiddle},
			{new Vector2Int(2, 0), ParticleAcceleratorType.FrontLeft},
			{new Vector2Int(2, 2), ParticleAcceleratorType.FrontRight}
		};

		#region LifeCycle

		private void Awake()
		{
			selfPart = GetComponent<ParticleAcceleratorPart>();
			registerTile = GetComponent<RegisterTile>();
			electricalNodeControl = GetComponent<ElectricalNodeControl>();

			damageData = ScriptableObject.CreateInstance<DamageData>();
			damageData.SetAttackType(AttackType.Rad);
			damageData.SetDamageType(DamageType.Clone);
		}

		private void Start()
		{
			if (selfPart.StartSetUp)
			{
				ConnectToParts();
			}
		}

		private void OnEnable()
		{
			UpdateManager.Add(AcceleratorUpdate, 1f);
			selfPart.OnShutDown.AddListener(OnMachineBreak);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, AcceleratorUpdate);
			selfPart.OnShutDown.RemoveListener(OnMachineBreak);
		}

		#endregion

		private void AcceleratorUpdate()
		{
			if(CustomNetworkManager.IsServer == false) return;

			if(connected == false) return;

			if(isOn == false && isAlwaysOn == false) return;

			var powerNeeded = voltageIncreasePerPowerLevel * ((int) CurrentState - 3);

			if (voltage < powerNeeded && isAlwaysOn == false)
			{
				powerUsage = "0";
				status = "<color=red>Not Enough Voltage</color>";
				UpdateGUI();
				return;
			}

			powerUsage = $"{powerNeeded}";
			UpdateGUI();

			if(DMMath.Prob(50)) return;

			ShootParticleAccelerator();
		}

		private void ShootParticleAccelerator()
		{
			var damageIntegrity = particleAcceleratorBulletPrefab.GetComponent<ProjectileDamageIntegrity>();
			damageData.SetDamage(10 * ((int)CurrentState - 2));
			damageIntegrity.damageData = damageData;

			foreach (var connectedPart in connectedParts)
			{
				if (connectedPart.ShootsBullet)
				{
					ProjectileManager.InstantiateAndShoot( particleAcceleratorBulletPrefab, orientation.LocalVector, connectedPart.gameObject,default);
				}
			}
		}

		#region ConnectToParts

		public void ConnectToParts()
		{
			if(connected) return;

			if (TryFindParts() == false)
			{
				//Failed to find all set up parts
				status = "<color=red>Not Connected</color>";
				UpdateGUI();
				return;
			}

			connected = true;

			foreach (var part in connectedParts)
			{
				part.OnShutDown.AddListener(OnMachineBreak);
			}

			ChangePower(ParticleAcceleratorState.Off);
		}

		private bool TryFindParts()
		{
			var enumValues = Enum.GetValues(typeof(OrientationEnum));
			bool correctArrangement = false;

			//For each set up direction
			foreach (var enumDirection in enumValues)
			{
				//Check to see if the system has been set up
				foreach (var section in machineBluePrint)
				{
					var coord = section.Key;
					for (int i = 1; i <= (int) enumDirection; i++)
					{
						coord = RotateVector90(coord).To2Int();
					}

					var objects = MatrixManager.GetAt<ParticleAcceleratorPart>(registerTile.WorldPositionServer + coord.To3Int() , true) as List<ParticleAcceleratorPart>;

					if (objects != null && objects.Count > 0 && section.Value == objects[0].ParticleAcceleratorType)
					{
						//Correct Part there woo but now check status
						if (objects[0].CurrentState == ParticleAcceleratorState.Frame || objects[0].CurrentState == ParticleAcceleratorState.Wired
						    || objects[0].Directional.CurrentDirection != (OrientationEnum) enumDirection || objects[0].ParticleAcceleratorType != section.Value)
						{
							//Frame or wired are not ready and isn't right direction so failed check
							correctArrangement = false;
							connectedParts.Clear();
							break;
						}

						//In right position and correct state and direction so this part passed, check other coords for faults now
						correctArrangement = true;
						connectedParts.Add(objects[0]);
						continue;
					}

					//Failed check for this direction as all parts need to be correct
					correctArrangement = false;
					connectedParts.Clear();
					break;
				}

				if (correctArrangement)
				{
					//This arrangement succeeded, dont need to check other directions
					orientation = Orientation.FromEnum((OrientationEnum) enumDirection);
					break;
				}
			}

			if (correctArrangement == false)
			{
				//No directions succeeded :(
				return false;
			}

			return true;
		}

		#endregion

		#region MachineBreaks

		private void OnMachineBreak(ParticleAcceleratorPart brokenPart)
		{
			connected = false;
			status = "<color=red>Not Connected</color>";

			foreach (var part in connectedParts)
			{
				part.OnShutDown.RemoveListener(OnMachineBreak);

				if(part == brokenPart) continue;

				part.ChangeState(ParticleAcceleratorState.Closed);
			}

			selfPart.ChangeState(ParticleAcceleratorState.Closed);

			connectedParts.Clear();
		}

		#endregion

		#region ChangePower

		/// <summary>
		/// Change power / turn On/OFF
		/// </summary>
		/// <param name="newState"></param>
		public void ChangePower(ParticleAcceleratorState newState)
		{
			if (connected == false)
			{
				powerUsage = "0";
				status = "<color=red>Not Connected</color>";
				UpdateGUI();
				return;
			}

			if(newState == ParticleAcceleratorState.Frame || newState == ParticleAcceleratorState.Wired || newState == ParticleAcceleratorState.Closed) return;

			if (newState == ParticleAcceleratorState.On3 && IsHacked == false)
			{
				newState = ParticleAcceleratorState.On2;
			}

			status = newState == ParticleAcceleratorState.Off ? "Off" : ((int)newState - 4).ToString();

			if (newState == ParticleAcceleratorState.On3)
			{
				status = "<color=red>3</color>";
			}

			if (voltage < voltageIncreasePerPowerLevel * ((int)newState - 3) && isAlwaysOn == false && newState != ParticleAcceleratorState.Off)
			{
				status = "<color=red>Not Enough Voltage</color>";
			}

			isOn = newState != ParticleAcceleratorState.Off;

			currentState = newState;

			foreach (var part in connectedParts)
			{
				part.ChangeState(newState);
			}

			selfPart.ChangeState(newState);

			UpdateGUI();
		}

		#endregion

		#region Interaction

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Emag);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			TryEmagController(interaction);
		}

		#endregion

		#region Hack

		//For now allow only emag hacking for setting 3
		private void TryEmagController(HandApply interaction)
		{
			if (isHacked)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, $"The {gameObject.ExpensiveName()} has already been hacked!");
				return;
			}

			Chat.AddActionMsgToChat(interaction.Performer, $"You attempt to hack the {gameObject.ExpensiveName()}, this will take around {timeToHack} seconds",
				$"{interaction.Performer.ExpensiveName()} starts hacking the {gameObject.ExpensiveName()}");

			var cfg = new StandardProgressActionConfig(StandardProgressActionType.Restrain);

			StandardProgressAction.Create(
				cfg,
				() => FinishHack(interaction)
			).ServerStartProgress(ActionTarget.Object(registerTile), timeToHack, interaction.Performer);

		}

		private void FinishHack(HandApply interaction)
		{
			if (DMMath.Prob(chanceToFailHack))
			{
				Chat.AddActionMsgToChat(interaction.Performer, $"Your attempt to hack the {gameObject.ExpensiveName()} failed",
					$"{interaction.Performer.ExpensiveName()} failed to hack the {gameObject.ExpensiveName()}");
				return;
			}

			Chat.AddActionMsgToChat(interaction.Performer, $"You hack the {gameObject.ExpensiveName()}",
				$"{interaction.Performer.ExpensiveName()} hacked the {gameObject.ExpensiveName()}");

			isHacked = true;
		}

		#endregion

		private void UpdateGUI()
		{
			var peppers = NetworkTabManager.Instance.GetPeepers(gameObject, NetTabType.ParticleAccelerator);
			if(peppers.Count == 0) return;

			List<ElementValue> valuesToSend = new List<ElementValue>();
			valuesToSend.Add(new ElementValue() { Id = "TextSetting", Value = Encoding.UTF8.GetBytes(status) });
			valuesToSend.Add(new ElementValue() { Id = "TextPower", Value = Encoding.UTF8.GetBytes(powerUsage + " volts") });
			valuesToSend.Add(new ElementValue() { Id = "SliderPower", Value = Encoding.UTF8.GetBytes(((int)(CurrentState - 3) * 100).ToString()) });

			// Update all UI currently opened.
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.ParticleAccelerator, TabAction.Update, valuesToSend.ToArray());
		}

		//Rotate vector 90 degrees ANTI-clockwise
		private Vector3Int RotateVector90(Vector2Int vector)
		{
			//Woo for easy math
			return new Vector3Int(-vector.y, vector.x, 0);
		}

		public void PowerNetworkUpdate()
		{
			voltage = electricalNodeControl.GetVoltage();
		}
	}
}
