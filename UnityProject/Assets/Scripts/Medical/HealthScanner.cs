using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthScanner : PickUpTrigger
{
	public void PlayerFound(PlayerHealth Playerhealth) {
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
		ChatRelay.Instance.AddToChatLogClient(ToShow, ChatChannel.Examine);
		//PostToChatMessage.Send(ToShow,ChatChannel.System);
		//Logger.Log(ToShow);
	}
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (gameObject == UIManager.Hands.CurrentSlot.Item)
		{
			Vector3 tposition = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
			tposition.z = 0f;
			List<PlayerHealth> objects = MatrixManager.GetAt<PlayerHealth>(tposition.RoundToInt());
			foreach (PlayerHealth theObject in objects) {
				PlayerFound(theObject);
			}
			return base.Interact(originator, position, hand);;
		}
		else
		{
			return base.Interact(originator, position, hand);
		}
	}
}
