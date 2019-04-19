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
		Logger.Log("oh cool");
		//if (!CanUse(originator, hand, position, false))
		//{
		//	return false;
		//}
		//if (!isServer)
		//{
		//	InteractMessage.Send(gameObject, hand);
		//}
		//else {
		if (gameObject == UIManager.Hands.CurrentSlot.Item)
		{
			position = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
			position.z = 0f;
			position = position.RoundToInt();
			Vector3 PlaceDirection = originator.transform.position - position;
			Connection WireEndB = Connection.NA;
			if (PlaceDirection == Vector3.up) {  WireEndB = Connection.North; }
			else if (PlaceDirection == Vector3.down) {  WireEndB = Connection.South; }
			else if (PlaceDirection == Vector3.right) {  WireEndB = Connection.East; }
			else if (PlaceDirection == Vector3.left) {  WireEndB = Connection.West; }
			 
			else if (PlaceDirection == Vector3.down + Vector3.left) {  WireEndB = Connection.SouthWest; }
			else if (PlaceDirection == Vector3.down + Vector3.right) {  WireEndB = Connection.SouthEast; }
			else if (PlaceDirection == Vector3.up + Vector3.left) {  WireEndB = Connection.NorthWest;  }
			else if (PlaceDirection == Vector3.up + Vector3.right) { WireEndB = Connection.NorthEast; }

			if (WireEndB != Connection.NA) {
				if (CableType == WiringColor.high) { 
					switch (WireEndB)
					{
						case  Connection.NorthEast:
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
		
			return true;
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
		Cable.GetComponent<CableInheritance>().SetDirection(WireEndB, WireEndA, CableType);
	}

	//public override void UI_Interact(GameObject originator, string hand)
	//{
	//	return base.UI_Interact(originator, hand);
	//}
}
