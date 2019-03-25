using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthScanner : PickUpTrigger
{
	public void PlayerFound(GameObject Player) {
		PlayerHealth Playerhealth = Player.GetComponent<PlayerHealth>();
		string ToShow = (Player.name + " is " + Playerhealth.ConsciousState.ToString() + "\n"
			+ "OverallHealth = " + Playerhealth.OverallHealth.ToString() + " Blood level = " + Playerhealth.bloodSystem.BloodLevel.ToString() + "\n"
						 + "Blood oxygen level = " + Playerhealth.bloodSystem.OxygenLevel.ToString() + "\n");
		string StringBuffer = "";
		float TotalBruteDamage = 0;
		float TotalBurnDamage = 0;
		float TotalOxygendamage = Playerhealth.CalculateOverallBloodLossDamage();
		foreach (BodyPartBehaviour BodyPart in Playerhealth.BodyParts)
		{
			StringBuffer += BodyPart.Type.ToString() + "\t";
			StringBuffer += BodyPart.BruteDamage.ToString() + "\t";
			TotalBruteDamage += BodyPart.BruteDamage;
			StringBuffer += BodyPart.BurnDamage.ToString();
			TotalBurnDamage += BodyPart.BurnDamage;
			StringBuffer += "\n";
		}
		ToShow = ToShow + "Overall, Brute " + TotalBruteDamage.ToString() + " Burn " + TotalBurnDamage.ToString() + " OxyLoss " + TotalOxygendamage.ToString() + "\n" + "Body Part, Brute, Burn \n" + StringBuffer;
		PostToChatMessage.Send(ToShow,ChatChannel.System); 
		//Logger.Log(ToShow);
	}
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		if (gameObject == UIManager.Hands.CurrentSlot.Item)
		{
			Vector3 tposition = Camera.main.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
			tposition.z = 0f;
			List<GameObject> objects = UITileList.GetItemsAtPosition(tposition);
			foreach (GameObject theObject in objects) {
				PlayerHealth thething = theObject.GetComponentInChildren<PlayerHealth>();
				if (thething != null) { 
                    PlayerFound(theObject);
				}
			}
			return base.Interact(originator, position, hand);;
		}
		else
		{
			return base.Interact(originator, position, hand);
		}
	}
}
