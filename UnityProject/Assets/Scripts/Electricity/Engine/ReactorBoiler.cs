using System;
using System.Collections;
using System.Collections.Generic;
using Pipes;
using UnityEngine;

public class ReactorBoiler : MonoBehaviour, ISetMultitoolMaster, ICheckedInteractable<HandApply>, IServerDespawn
{
	public decimal MaxPressureInput = 630000M;
	public decimal CurrentPressureInput = 0;
	public decimal OutputEnergy;
	public decimal TotalEnergyInput;

	public decimal Efficiency = 0.5M;

	public ReactorPipe ReactorPipe;

	//public ReactorTurbine reactorTurbine;
	public List<ReactorGraphiteChamber> Chambers;

	private RegisterObject registerObject;
	// Start is called before the first frame update


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

	public void Awake()
	{
		ReactorPipe = GetComponent<ReactorPipe>();
		registerObject = GetComponent<RegisterObject>();
	}

	public void CycleUpdate()
	{
		//Maybe change equation later to something cool
		CurrentPressureInput = 0;
		CurrentPressureInput = (decimal) ((ReactorPipe.pipeData.mixAndVolume.Mix.InternalEnergy - ReactorPipe.pipeData.mixAndVolume.Mix.WholeHeatCapacity * 293.15f));
		if (CurrentPressureInput > 0)
		{
			ReactorPipe.pipeData.mixAndVolume.Mix.InternalEnergy -= (float) (CurrentPressureInput * Efficiency);

			//Logger.Log("CurrentPressureInput " + CurrentPressureInput);
			if (CurrentPressureInput > MaxPressureInput)
			{
				Logger.LogError(" ReactorBoiler !!!booommmm!!", Category.Editor);
				Explosions.Explosion.StartExplosion(registerObject.LocalPosition, 800, registerObject.Matrix);
			}

			OutputEnergy = CurrentPressureInput * Efficiency;
		}
		else
		{
			OutputEnergy = 0;
		}
	}

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
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
				$"{interaction.Performer.ExpensiveName()} deconstruct the ReactorBoiler.",
				() => { Despawn.ServerSingle(gameObject); });
		}
	}

	/// <summary>
	/// is the function to denote that it will be pooled or destroyed immediately after this function is finished, Used for cleaning up anything that needs to be cleaned up before this happens
	/// </summary>
	void IServerDespawn.OnDespawnServer(DespawnInfo info)
	{
		Spawn.ServerPrefab(CommonPrefabs.Instance.Metal, this.GetComponent<RegisterObject>().WorldPositionServer,
			count: 15);
	}


	//######################################## Multitool interaction ##################################
	private MultitoolConnectionType conType = MultitoolConnectionType.BoilerTurbine;
	public MultitoolConnectionType ConType => conType;
	private bool multiMaster = false;
	public bool MultiMaster => multiMaster;

	public void AddSlave(object SlaveObjectThis)
	{
	}
}