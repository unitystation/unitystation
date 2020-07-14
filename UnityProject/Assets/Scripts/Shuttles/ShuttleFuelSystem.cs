using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Atmospherics;
using Mirror;


/// <summary>
/// Used to monitor the fuel level and to remove fuel from the canister and also stop the shuttle if the fuel has run out
/// </summary>
public class ShuttleFuelSystem : ManagedNetworkBehaviour
{
	public float FuelLevel;
	public ShuttleFuelConnector Connector;
	public MatrixMove MatrixMove;
	public float FuelConsumption;
	public float massConsumption = 2.5f;

	public float CalculatedMassConsumption;

	public float OptimumMassConsumption = 0.5f;


	protected override void OnEnable() {
		base.OnEnable();
		if (MatrixMove == null) {
			MatrixMove = this.GetComponent<MatrixMove>();
			MatrixMove.RegisterShuttleFuelSystem(this);
		}
	}

	public override void UpdateMe()
	{

		if (this.Connector.canister != null)
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
			FuelLevel = Connector.canister.container.GasMix.GetMoles(Gas.Plasma) / 60000;
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
		var Plasma = Connector.canister.container.GasMix.GetMoles(Gas.Plasma);
		var Oxygen = Connector.canister.container.GasMix.GetMoles(Gas.Oxygen);
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
		if (CMassconsumption > 6)
		{
			CMassconsumption = 6;
		}
		//Logger.Log("Ratio3 > " + Ratio);
		CalculatedMassConsumption = (CMassconsumption * OptimumMassConsumption * FuelConsumption);

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
			//Logger.Log("CalculatedMassConsumption > " + CalculatedMassConsumption*massConsumption);
			Connector.canister.container.GasMix = Connector.canister.container.GasMix.RemoveGasReturn(Gas.Plasma, CalculatedMassConsumption * massConsumption  * (0.7f));
			Connector.canister.container.GasMix = Connector.canister.container.GasMix.RemoveGasReturn(Gas.Oxygen, CalculatedMassConsumption * massConsumption  * (0.3f));
		}
		else if (Connector.canister.container.GasMix.GetMoles(Gas.Plasma) > massConsumption * FuelConsumption)
		{
			//Logger.Log("Full-back > " + (FuelConsumption * massConsumption));
			Connector.canister.container.GasMix = Connector.canister.container.GasMix.RemoveGasReturn(Gas.Plasma, (massConsumption * FuelConsumption));
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
		else if (Connector.canister.container.GasMix.GetMoles(Gas.Plasma) > massConsumption * FuelConsumption)
		{
			return (true);
		}
		return (false);
	}
}
