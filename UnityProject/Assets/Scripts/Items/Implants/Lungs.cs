using System.Collections;
using System.Collections.Generic;
using Systems.Atmospherics;
using HealthV2;
using Objects.Atmospherics;
using UnityEngine;

public class Lungs : ImplantBase
{
	[SerializeField]
	private int breatheCooldown = 4;

	[SerializeField]
	private float reagentSafeMin = 16;

	[SerializeField]
	private Gas requiredGas = Gas.Oxygen;

	[SerializeField]
	private Gas expelledGas = Gas.CarbonDioxide;

	private bool isSuffocating = false;
	public bool IsSuffocating => isSuffocating;

	public override void ImplantUpdate(LivingHealthMasterBase healthMaster)
	{
		base.ImplantUpdate(healthMaster);

		Vector3Int position = healthMaster.OBehavior.AssumedWorldPositionServer();
		MetaDataNode node = MatrixManager.GetMetaDataAt(position);

		if (Breathe(node, healthMaster))
		{
			AtmosManager.Update(node);
		}
	}

	private bool Breathe(IGasMixContainer node, LivingHealthMasterBase healthMaster)
	{
		breatheCooldown --; //not timebased, but tickbased
		if(breatheCooldown > 0){
			return false;
		}

		if (!healthMaster.CirculatorySystem) //No point breathing if we dont have blood.
		{
			return false;
		}

		//TODO: This should also make sure that the circulatory system accepts the type of gas these lungs do!

		// if no internal breathing is possible, get the from the surroundings
		IGasMixContainer container = node;
		if (healthMaster is PlayerHealthV2 playerHealth)
		{
			container= GetInternalGasMix(playerHealth) ?? node;
		}


		//Can probably edit this to use the volume of the lungs instead.
		GasMix gasMix = container.GasMix;
		GasMix breathGasMix = gasMix.RemoveVolume(AtmosConstants.BREATH_VOLUME, true);

		float reagentUsed = HandleBreathing(breathGasMix, healthMaster);

		if (reagentUsed > 0)
		{
			breathGasMix.RemoveGas(requiredGas, reagentUsed);
			node.GasMix.AddGas(expelledGas, reagentUsed);
			healthMaster.RegisterTile.Matrix.MetaDataLayer.UpdateSystemsAt(healthMaster.RegisterTile.LocalPositionClient, SystemType.AtmosSystem);
			
			healthMaster.CirculatorySystem.AddBloodReagent(reagentUsed);

		}

		gasMix += breathGasMix;
		container.GasMix = gasMix;

		return reagentUsed > 0;
	}

	//A bit hacky, get the gas mask the player is wearing if they have one.
	private GasContainer GetInternalGasMix(PlayerHealthV2 playerHealth)
	{
		PlayerScript playerScript = playerHealth.RegPlayer.PlayerScript;
		if (playerScript != null)
		{
			// Check if internals exist
			var maskItemAttrs = playerScript.ItemStorage.GetNamedItemSlot(NamedSlot.mask).ItemAttributes;
			bool internalsEnabled = playerHealth.Equip.IsInternalsEnabled;
			if (maskItemAttrs != null && maskItemAttrs.CanConnectToTank && internalsEnabled)
			{
				foreach ( var gasSlot in playerScript.ItemStorage.GetGasSlots() )
				{
					if (gasSlot.Item == null) continue;
					var gasContainer = gasSlot.Item.GetComponent<GasContainer>();
					if ( gasContainer )
					{
						return gasContainer;
					}
				}
			}
		}

		return null;
	}

	private float HandleBreathing(GasMix breathGasMix, LivingHealthMasterBase healthMaster)
	{
		float reagentPressure = breathGasMix.GetPressure(requiredGas);

		float reagentUsed = 0;

		if (reagentPressure < reagentSafeMin)
		{
			if (Random.value < 0.1)
			{
				Chat.AddActionMsgToChat(gameObject, "You gasp for breath", $"{gameObject.name} gasps");
			}

			if (reagentPressure > 0)
			{
				float ratio = 1 - reagentPressure / reagentSafeMin;
				//bloodSystem.OxygenDamage += 1 * ratio;
				reagentUsed = breathGasMix.GetMoles(requiredGas) * ratio;
			}
			else
			{
				//bloodSystem.OxygenDamage += 1;
			}
			isSuffocating = true;
		}
		else
		{
			reagentUsed = breathGasMix.GetMoles(Gas.Oxygen);
			isSuffocating = false;
			//bloodSystem.OxygenDamage -= 2.5f;
			breatheCooldown = 4;
		}
		return reagentUsed;
	}

}
