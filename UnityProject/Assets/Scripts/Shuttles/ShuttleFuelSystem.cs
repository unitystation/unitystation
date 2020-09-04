using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;
using Mirror;
using Pipes;

/// <summary>
/// Used to monitor the fuel level and to remove fuel from the canister and also stop the shuttle if the fuel has run out
/// </summary>
public class ShuttleFuelSystem : ManagedNetworkBehaviour
{
	public float FuelLevel;
	public ShuttleFuelConnector Connector;
	public MatrixMove MatrixMove;
	public float FuelConsumption;
	public float MassConsumption = 0.1f;

	public float CalculatedMassConsumption;

	public float optimumMassConsumption = 0.05f;


	protected override void OnEnable() {
		base.OnEnable();
		if (MatrixMove == null) {
			MatrixMove = this.GetComponent<MatrixMove>();
			MatrixMove.RegisterShuttleFuelSystem(this);
		}
	}

	public override void UpdateMe()
	{

		if (Connector.canister != null)
		{
			FuelConsumption =  MatrixMove.ServerState.Speed / 25f;
			if (MatrixMove.IsMovingServer && MatrixMove.RequiresFuel)
			{
				FuelCalculations();
			}
			if (IsFuelled())
			{
				MatrixMove.IsFueled = true;
			}
			FuelLevel = Connector.canister.GasContainer.GasMix.GetMoles(Gas.Plasma) / 60000;
		}
		else
		{
			FuelLevel = 0f;
			MatrixMove.IsFueled = false;
			if (MatrixMove.IsMovingServer && MatrixMove.RequiresFuel)
			{
				MatrixMove.StopMovement();
			}
		}
		if (FuelLevel > 1)
		{
			FuelLevel = 1;
		}
		//Logger.Log("FuelLevel " + FuelLevel.ToString());
	}

	bool IsFuelledOptimum()
	{
		var Plasma = Connector.canister.GasContainer.GasMix.GetMoles(Gas.Plasma);
		var Oxygen = Connector.canister.GasContainer.GasMix.GetMoles(Gas.Oxygen);
		var Ratio = ((Plasma / Oxygen) / (7f / 3f));
		//Logger.Log("Ratio > " + Ratio);
		Ratio = Ratio * 2f;
		//Logger.Log("Ratio1 > " + Ratio);
		if (Ratio > 1)
		{
			Ratio = Ratio - 1;
		}
		else {
			Ratio = Ratio / 5f;
		}

		if (Ratio > 1)
		{
			Ratio = 1f / Ratio;
		}
		//Logger.Log("Ratio2 > " + Ratio);

		var CMassconsumption = 1f / Ratio;
		if (CMassconsumption > 1)
		{
			CMassconsumption = 1;
		}
		//Logger.Log("Ratio3 > " + Ratio);
		CalculatedMassConsumption = (CMassconsumption * optimumMassConsumption * FuelConsumption);

		if ((Plasma > (CalculatedMassConsumption)  * (0.7f)) && (Oxygen > (CalculatedMassConsumption)  * (0.3f)))
		{

			return (true);
		}
		return (false);

	}

	void FuelCalculations()
	{
		if (IsFuelledOptimum())
		{
			//Logger.Log("CalculatedMassConsumption > " + CalculatedMassConsumption*MassConsumption);
			Connector.canister.GasContainer.GasMix = Connector.canister.GasContainer.GasMix.RemoveGasReturn(Gas.Plasma, CalculatedMassConsumption * MassConsumption  * (0.7f));
			Connector.canister.GasContainer.GasMix = Connector.canister.GasContainer.GasMix.RemoveGasReturn(Gas.Oxygen, CalculatedMassConsumption * MassConsumption  * (0.3f));
		}
		else if (Connector.canister.GasContainer.GasMix.GetMoles(Gas.Plasma) > MassConsumption * FuelConsumption)
		{
			//Logger.Log("Full-back > " + (FuelConsumption * MassConsumption));
			Connector.canister.GasContainer.GasMix = Connector.canister.GasContainer.GasMix.RemoveGasReturn(Gas.Plasma, (MassConsumption * FuelConsumption));
		}
		else {
			MatrixMove.IsFueled = false;
			MatrixMove.StopMovement();
		}

	}
	bool IsFuelled()
	{
		if (IsFuelledOptimum()) {
			return (true);
		}
		else if (Connector.canister.GasContainer.GasMix.GetMoles(Gas.Plasma) > MassConsumption * FuelConsumption)
		{
			return (true);
		}
		return (false);
	}
}
