using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MachineConnectorSpriteHandler : NetworkBehaviour
{
	public List<PowerTypeCategory> CanConnectTo = new List<PowerTypeCategory>();
	public HashSet<PowerTypeCategory> HashSetCanConnectTo;
	public char InNorth = '0';
	public char InSouth = '0';
	public char InWest = '0';
	public char InEast = '0';

	public SpriteRenderer SRNorth;
	public SpriteRenderer SRSouth;
	public SpriteRenderer SRWest;
	public SpriteRenderer SREast;

	[SyncVar(hook = nameof(UpdateSprites))]
	public string Syncstring;

	public WireConnect Wire;

	public void Check()
	{
		if (HashSetCanConnectTo == null)
		{
			HashSetCanConnectTo = new HashSet<PowerTypeCategory>(CanConnectTo);
		}

		InNorth = '0';
		InSouth = '0';
		InWest = '0';
		InEast = '0';
		Syncstring = "0000";
		HashSet<IntrinsicElectronicData> connections = new HashSet<IntrinsicElectronicData>();
		ElectricityFunctions.SwitchCaseConnections(Wire.transform.localPosition,
			Wire.Matrix, HashSetCanConnectTo,
			Connection.MachineConnect, Wire.InData,
			connections);
		foreach (var cn in connections)
		{
			Vector3 v3 = (cn.Present.transform.localPosition - Wire.transform.localPosition).CutToInt();
			if (v3 == Vector3.up)
			{
				InNorth = '1';
			}
			else if (v3 == Vector3.down)
			{
				InSouth = '1';
			}
			else if (v3 == Vector3.right)
			{
				InEast = '1';
			}
			else if (v3 == Vector3.left)
			{
				InWest = '1';
			}
		}

		Syncstring = InNorth.ToString() + InSouth.ToString() + InWest.ToString() + InEast.ToString();
		UpdateSprites(null, Syncstring);
	}

	public override void OnStartClient()
	{
		UpdateSprites(null, Syncstring);
	}

	public void UpdateSprites(string oldStates, string states)
	{
		if (states.Length != 4) return;

		SRNorth.enabled = (states[0] == '1');
		SRSouth.enabled = (states[1] == '1');
		SRWest.enabled = (states[2] == '1');
		SREast.enabled = (states[3] == '1');
	}
}