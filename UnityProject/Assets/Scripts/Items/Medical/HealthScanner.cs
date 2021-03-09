using System.Collections;
using System.Collections.Generic;
using HealthV2;
using UnityEngine;

/// <summary>
/// Main health scanner interaction. Applying it to a living thing sends a request to the server to
/// tell us their health info.
/// </summary>
public class HealthScanner : MonoBehaviour, ICheckedInteractable<HandApply>
{
	public bool AdvancedHealthScanner = false;

	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can only be applied to LHB
		if (!Validations.HasComponent<LivingHealthMasterBase>(interaction.TargetObject)) return false;
		return true;
	}

	public void ServerPerformInteraction(HandApply interaction)
	{
		var livingHealth = interaction.TargetObject.GetComponent<LivingHealthMasterBase>();
		string ToShow = (livingHealth.name + " is " + livingHealth.ConsciousState.ToString() + "\n"
		                 + "OverallHealth = " + livingHealth.OverallHealth.ToString() + " Blood level = " +
		                 livingHealth.CirculatorySystem.UseBloodPool.ToString() + "\n");
		string StringBuffer = "";
		float TotalBruteDamage = 0;
		float TotalBurnDamage = 0;
		float TotalTOXDamage = 0;
		float TotalCloneDamage = 0;
		foreach (var BodyPart in livingHealth.ImplantList)
		{
			if (AdvancedHealthScanner == false && BodyPart.DamageContributesToOverallHealth == false) continue;

			if (BodyPart.DamageContributesToOverallHealth)
			{
				TotalBruteDamage += BodyPart.Brute;
				TotalBurnDamage += BodyPart.Burn;
				TotalTOXDamage += BodyPart.Toxin;
				TotalCloneDamage += BodyPart.Cellular;
			}

			StringBuffer += BodyPart.name + "\t";
			StringBuffer += BodyPart.Brute + "\t";
			StringBuffer += BodyPart.Burn + "\t";
			StringBuffer += BodyPart.Toxin + "\t";
			StringBuffer += BodyPart.Cellular;

			StringBuffer += "\n";
		}

		ToShow = ToShow + "Overall, Brute " + TotalBruteDamage.ToString() + " Burn " + TotalBurnDamage.ToString() +
		         " Toxin " + TotalTOXDamage +
		         " Cellular " + TotalCloneDamage + "\n" +
		         "Body Part, Brute, Burn, Toxin, Cellular \n" +
		         StringBuffer;

		Chat.AddExamineMsgFromServer(interaction.Performer, ToShow);
	}
}