using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MachineConnectorSpriteHandler : NetworkBehaviour
{
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
		InNorth = '0';
		InSouth = '0';
		InWest = '0';
		InEast = '0';
		Syncstring = "0000";
		HashSet<ElectricalOIinheritance> connections = new HashSet<ElectricalOIinheritance>();
		connections = ElectricityFunctions.SwitchCaseConnections(Wire.transform.localPosition, Wire.Matrix, Wire.InData.CanConnectTo, Connection.MachineConnect, Wire);
		foreach (var cn in connections)
		{
			Vector3 v3 = (cn.transform.localPosition - Wire.transform.localPosition).CutToInt();
			if (v3 == Vector3.up) { InNorth = '1'; }
			else if (v3 == Vector3.down) { InSouth = '1'; }
			else if (v3 == Vector3.right) { InEast = '1'; }
			else if (v3 == Vector3.left) { InWest = '1'; }
		}
		Syncstring = InNorth.ToString() + InSouth.ToString() + InWest.ToString() + InEast.ToString();
		UpdateSprites(null, Syncstring);
	}
	public override void OnStartClient() {
		UpdateSprites(null, Syncstring);
	}
	public void UpdateSprites(string oldStates, string states)
	{
		for (int i = 0; i < states.Length; i++)
		{
			if (i == 0){
				if (states[i] == '1'){SRNorth.enabled = true;}
				else {SRNorth.enabled = false;}}
			else if (i == 1){
				if (states[i] == '1'){SRSouth.enabled = true;}
				else {SRSouth.enabled = false;}}
			else if (i == 2) {
				if (states[i] == '1'){SRWest.enabled = true;}
				else {SRWest.enabled = false;}}
			else if (i == 3){
				if (states[i] == '1'){SREast.enabled = true;}
				else {SREast.enabled = false;}}
		}
	}
}
