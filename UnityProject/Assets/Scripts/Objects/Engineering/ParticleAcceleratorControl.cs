using System;
using System.Collections.Generic;
using UnityEngine;
using Weapons;
using Weapons.Projectiles.Behaviours;

namespace Objects.Engineering
{
	[RequireComponent(typeof(ParticleAcceleratorPart))]
	public class ParticleAcceleratorControl : MonoBehaviour, INodeControl
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
		private float voltageIncreasePerPowerLevel = 200;

		[SerializeField]
		private GameObject particleAcceleratorBulletPrefab = null;

		[SerializeField]
		private bool isAlwaysOn;

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

			if(voltage < voltageIncreasePerPowerLevel * ((int)CurrentState - 3) && isAlwaysOn == false) return;

			if(DMMath.Prob(50)) return;

			ShootParticleAccelerator();
		}

		private void ShootParticleAccelerator()
		{
			var damageIntegrity = particleAcceleratorBulletPrefab.GetComponent<ProjectileDamageIntegrity>();

			damageIntegrity.damageOverride = true;
			damageIntegrity.damageOverrideValue = 20 * ((int)CurrentState - 3);

			foreach (var connectedPart in connectedParts)
			{
				if (connectedPart.ShootsBullet)
				{
					CastProjectileMessage.SendToAll(connectedPart.gameObject, particleAcceleratorBulletPrefab, orientation.Vector, default);
				}
			}
		}

		#region ConnectToParts

		public void ConnectToParts()
		{
			if (TryFindParts() == false)
			{
				//Failed to find all set up parts
				status = "<color=red>Not Connected</color>";
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
					var objects = MatrixManager.GetAt<ParticleAcceleratorPart>(registerTile.WorldPositionServer + (RotateVector90(section.Key) * (int) enumDirection), true);

					if (objects.Count > 0 && section.Value == objects[0].ParticleAcceleratorType)
					{
						//Correct Part there woo but now check status
						if (objects[0].CurrentState == ParticleAcceleratorState.Frame || objects[0].CurrentState == ParticleAcceleratorState.Wired
						    || objects[0].Directional.CurrentDirection.AsEnum() != (OrientationEnum) enumDirection || objects[0].ParticleAcceleratorType != section.Value)
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
			if(connected == false) return;

			if(newState == ParticleAcceleratorState.Frame || newState == ParticleAcceleratorState.Wired || newState == ParticleAcceleratorState.Closed) return;

			status = newState == ParticleAcceleratorState.Off ? "Off" : ((int)newState - 4).ToString();

			Debug.LogError($"current voltage: {voltage}, needed voltage: {voltageIncreasePerPowerLevel * ((int)CurrentState - 3)}");

			if (voltage < voltageIncreasePerPowerLevel * ((int)CurrentState - 3) && isAlwaysOn == false && newState != ParticleAcceleratorState.Off)
			{
				newState = ParticleAcceleratorState.Off;
				status = "<color=red>Not Enough Voltage</color>";
			}


			isOn = newState != ParticleAcceleratorState.Off;

			currentState = newState;

			foreach (var part in connectedParts)
			{
				part.ChangeState(newState);
			}

			selfPart.ChangeState(newState);
		}

		#endregion

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
