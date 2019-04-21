using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableCoil : PickUpTrigger
{
	public WiringColor CableType; 
	public GameObject CablePrefab;
    void Start()
    {
        
    }

    void Update()
    {
        
    }
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//Logger.LogError("oh cool");
		//if (!CanUse(originator, hand, position, false))
		//{
		//	return false;
		//}
		Logger.Log(originator + " " + position + " " + hand);
		if (!isServer)
		{
			InteractMessage.Send(gameObject, position , hand);
		}
		else {

			PlayerNetworkActions pna = originator.GetComponent<PlayerNetworkActions>();
			GameObject handObj = pna.Inventory[hand].Item;
			if (handObj == null)
			{
				return base.Interact(originator, position, hand);
			}
			if (handObj.GetComponent<CableCoil>())
			{
			//if ( UIManager.Hands.CurrentSlot.Item.GetComponent<CableCoil>() != null)
			//{
				//position = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
				position.z = 0f;
				position = position.RoundToInt();
				Vector3 PlaceDirection = originator.transform.position - position;
				Logger.Log(PlaceDirection.ToString());
				Connection WireEndB = Connection.NA;
				if (PlaceDirection == Vector3.up) { WireEndB = Connection.North; }
				else if (PlaceDirection == Vector3.down) { WireEndB = Connection.South; }
				else if (PlaceDirection == Vector3.right) { WireEndB = Connection.East; }
				else if (PlaceDirection == Vector3.left) { WireEndB = Connection.West; }

				else if (PlaceDirection == Vector3.down + Vector3.left) { WireEndB = Connection.SouthWest; }
				else if (PlaceDirection == Vector3.down + Vector3.right) { WireEndB = Connection.SouthEast; }
				else if (PlaceDirection == Vector3.up + Vector3.left) { WireEndB = Connection.NorthWest; }
				else if (PlaceDirection == Vector3.up + Vector3.right) { WireEndB = Connection.NorthEast; }

				if (WireEndB != Connection.NA)
				{
					if (CableType == WiringColor.high)
					{
						switch (WireEndB)
						{
							case Connection.NorthEast:
								return true;
							case Connection.NorthWest:
								return true;
							case Connection.SouthWest:
								return true;
							case Connection.SouthEast:
								return true;
						}

					}
					Logger.Log(WireEndB.ToString());
					BuildCable(position, originator.transform.parent, WireEndB);
				}
				//Logger.Log(position.ToString());
			} 
			return base.Interact(originator, position, hand);
		}

		//}


		return base.Interact(originator, position, hand);
	}
	//[Server]
	private void BuildCable(Vector3 position, Transform parent, Connection WireEndB)
	{
		Logger.Log("YOYOYO");
		GameObject Cable = PoolManager.PoolNetworkInstantiate(CablePrefab, position, parent);
		//DisappearObject();
		Connection WireEndA = Connection.Overlap;
		//GameObject cable, Connection WireEndA, Connection WireEndB, WiringColor CableType
		ElectricalCableMessage.Send(Cable, WireEndA, WireEndB, CableType);
		Cable.GetComponent<CableInheritance>().SetDirection(WireEndB, WireEndA, CableType);
	}

	//public override void UI_Interact(GameObject originator, string hand)
	//{
	//	return base.UI_Interact(originator, hand);
	//}
}
