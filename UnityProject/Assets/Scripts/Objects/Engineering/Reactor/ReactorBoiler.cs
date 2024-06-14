using System;
using System.Collections.Generic;
using Systems.Atmospherics;
using UnityEngine;
using ScriptableObjects;
using Objects.Atmospherics;
using Shared.Systems.ObjectConnection;


namespace Objects.Engineering
{
	public class ReactorBoiler : MonoBehaviour, IMultitoolMasterable, ICheckedInteractable<HandApply>, IServerDespawn
	{
		public decimal MaxPressureInput = 130000M;
		public decimal CurrentPressureInput = 0;
		public decimal OutputEnergy;
		public decimal TotalEnergyInput;

		public decimal Efficiency = 0.825M;
		private const int BOILING_TEMP = 100;

		public ReactorPipe ReactorPipe;

		public List<ReactorGraphiteChamber> Chambers;
		// Start is called before the first frame update
		[field: SerializeField] public bool CanRelink { get; set; } = true;
		[field: SerializeField] public bool IgnoreMaxDistanceMapper { get; set; } = false;

		[SerializeField, Range(0, 1), Tooltip("The % cooling of this boiler per update. 100% cools input gas immediately to 100 degrees. 0% doesn't cool the gas.")] private float coolingRate = 0.5f;

		#region Lifecycle

		public void Awake()
		{
			ReactorPipe = GetComponent<ReactorPipe>();
		}

		private void OnEnable()
		{
			if (CustomNetworkManager.Instance._isServer == false) return;

			UpdateManager.Add(CycleUpdate, 1);
		}

		private void OnDisable()
		{
			if (CustomNetworkManager.Instance._isServer == false) return;

			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, CycleUpdate);
		}

		/// <summary>
		/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
		/// </summary>
		void IServerDespawn.OnDespawnServer(DespawnInfo info)
		{
			Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, this.GetComponent<RegisterObject>().WorldPositionServer,
				count: 15);
		}

		#endregion

		public void CycleUpdate()
		{
			//Maybe change equation later to something cool
			CurrentPressureInput = 0;
			var ExpectedInternalEnergy = ReactorPipe.pipeData.mixAndVolume.WholeHeatCapacity * (Reactions.KOffsetC + BOILING_TEMP);

			var InternalEnergy = ReactorPipe.pipeData.mixAndVolume.InternalEnergy;

			CurrentPressureInput = (decimal)(InternalEnergy - ExpectedInternalEnergy) * (decimal)coolingRate;

			if (CurrentPressureInput > 0)
			{
				//Loggy.Log("CurrentPressureInput " + CurrentPressureInput);
				// if (CurrentPressureInput > MaxPressureInput)
				// {
				// 	CurrentPressureInput = MaxPressureInput;
				// 	//Loggy.LogError(" ReactorBoiler !!!booommmm!!", Category.Editor);
				// 	//Explosions.Explosion.StartExplosion(registerObject.LocalPosition, 800, registerObject.Matrix);
				// }


				ReactorPipe.pipeData.mixAndVolume.InternalEnergy = InternalEnergy - (float)CurrentPressureInput;
				OutputEnergy = CurrentPressureInput * Efficiency; //Only half of the energy is converted into useful energy
			}
			else
			{
				OutputEnergy = 0;
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (!Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder)) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Welder))
			{
				ToolUtils.ServerUseToolWithActionMessages(interaction, 10,
					"You start to deconstruct the ReactorBoiler..",
					$"{interaction.Performer.ExpensiveName()} starts to deconstruct the ReactorBoiler...",
					"You deconstruct the ReactorBoiler",
					$"{interaction.Performer.ExpensiveName()} deconstructs the ReactorBoiler.",
					() => { _ = Despawn.ServerSingle(gameObject); });
			}
		}

		#region Multitool Interaction

		public MultitoolConnectionType ConType => MultitoolConnectionType.BoilerTurbine;
		public bool MultiMaster => false;
		int IMultitoolMasterable.MaxDistance => int.MaxValue;

		#endregion
	}
}
