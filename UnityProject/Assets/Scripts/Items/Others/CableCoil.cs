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
		//Logger.Log(originator + " " + position + " " + hand);
		if (!isServer)
		{
			InteractMessage.Send(gameObject, position , hand);
		}
		else {
			var slot = InventoryManager.GetSlotFromOriginatorHand(originator, hand);
			var CableCoil_ = slot.Item?.GetComponent<CableCoil>();

			if (CableCoil_ != null)
			{
				position.z = 0f;
				position = position.RoundToInt();
				Vector3 PlaceDirection = originator.transform.position - position;
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

					var worldPosInt = position.CutToInt();
					var matrix = MatrixManager.AtPoint(worldPosInt);
					var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrix);

					var econs = originator.GetComponentInParent<Matrix>().GetElectricalConnections(localPosInt);
					foreach (var con in econs) {
						if (con.WireEndA == Connection.Overlap || con.WireEndB == Connection.Overlap) {
							if (con.WireEndA == WireEndB || con.WireEndB == WireEndB)
							{
								ChatRelay.Instance.AddToChatLogClient("There is already a cable at that position", ChatChannel.Examine);
								return true;
							}
							foreach (var Econ in econs)
							{
								if (Econ.WireEndA == WireEndB || Econ.WireEndB == WireEndB)
								{
									if (con.WireEndA == Econ.WireEndA || con.WireEndB == Econ.WireEndA){
										ChatRelay.Instance.AddToChatLogClient("There is already a cable at that position", ChatChannel.Examine);
										return true;
									}
									else if (con.WireEndA == Econ.WireEndB || con.WireEndB == Econ.WireEndB){
										ChatRelay.Instance.AddToChatLogClient("There is already a cable at that position", ChatChannel.Examine);
										return true;
									}								} 
							}
						}
					}
					BuildCable(position, originator.transform.parent, WireEndB);
				}
			} 
			return base.Interact(originator, position, hand);
		}
		return base.Interact(originator, position, hand);
	}
	private void BuildCable(Vector3 position, Transform parent, Connection WireEndB)
	{
		Connection WireEndA = Connection.Overlap;
		GameObject Cable = PoolManager.PoolNetworkInstantiate(CablePrefab, position, parent);
		ElectricalCableMessage.Send(Cable, WireEndA, WireEndB, CableType);
		Cable.GetComponent<CableInheritance>().SetDirection(WireEndB, WireEndA, CableType);
	}
}
