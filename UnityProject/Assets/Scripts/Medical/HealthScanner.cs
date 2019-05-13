using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthScanner : PickUpTrigger
{
	public override void Attack(GameObject target, GameObject originator, BodyPartType bodyPart)
	{
		var playerHealth = target.GetComponent<PlayerHealth>();
		PlayerFound(playerHealth, originator);
	}

	public void PlayerFound(PlayerHealth Playerhealth, GameObject originator) {
		string ToShow = (Playerhealth.name + " is " + Playerhealth.ConsciousState.ToString() + "\n"
			+ "OverallHealth = " + Playerhealth.OverallHealth.ToString() + " Blood level = " + Playerhealth.bloodSystem.BloodLevel.ToString() + "\n"
						 + "Blood levels = " + Playerhealth.CalculateOverallBloodLossDamage() + "\n");
		string StringBuffer = "";
		float TotalBruteDamage = 0;
		float TotalBurnDamage = 0;
		foreach (BodyPartBehaviour BodyPart in Playerhealth.BodyParts)
		{
			StringBuffer += BodyPart.Type.ToString() + "\t";
			StringBuffer += BodyPart.BruteDamage.ToString() + "\t";
			TotalBruteDamage += BodyPart.BruteDamage;
			StringBuffer += BodyPart.BurnDamage.ToString();
			TotalBurnDamage += BodyPart.BurnDamage;
			StringBuffer += "\n";
		}
		ToShow = ToShow + "Overall, Brute " + TotalBruteDamage.ToString() + " Burn " + TotalBurnDamage.ToString() + " OxyLoss " + Playerhealth.bloodSystem.OxygenDamage.ToString() + "\n" + "Body Part, Brute, Burn \n" + StringBuffer;
		UpdateChatMessage.Send(originator, ChatChannel.Examine, ToShow);
	}
}
