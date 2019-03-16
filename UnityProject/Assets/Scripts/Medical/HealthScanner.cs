using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthScanner : PickUpTrigger
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
	public void PlayerFound(GameObject Player) {
		PlayerHealth Playerhealth = Player.GetComponent<PlayerHealth>();
		string ToShow = (Player.name + " is " + Playerhealth.ConsciousState.ToString() + "\n" 
			+"OverallHealth = " + Playerhealth.OverallHealth.ToString() + " Blood level = " + Playerhealth.bloodSystem.BloodLevel.ToString() +  "\n" 
		                 + "Blood oxygen level = " + Playerhealth.bloodSystem.OxygenLevel.ToString() + "\n"
		                 + "Body Part, Brut, Burn \n");
		foreach (BodyPartBehaviour BodyPart in Playerhealth.BodyParts) {
			ToShow += BodyPart.Type.ToString() + "\t";
			ToShow += BodyPart.BruteDamage.ToString() + "\t";
			ToShow += BodyPart.BurnDamage.ToString();
			ToShow += "\n";
		}

		PostToChatMessage.Send(ToShow,ChatChannel.System); 
		Logger.Log(ToShow);
	}

	/// <summary>
	/// Occurs when shooting by clicking on empty space
	/// </summary>
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//shoot gun interation if its in hand
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
			Logger.Log("yo");
            //
			return base.Interact(originator, position, hand);;
		}
		else
		{
			return base.Interact(originator, position, hand);
		}
	}
}
