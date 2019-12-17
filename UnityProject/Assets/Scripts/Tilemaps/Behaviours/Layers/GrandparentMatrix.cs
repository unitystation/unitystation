
using Mirror;
using UnityEngine;

/// <summary>
/// This component lives on the parent transform of Matrix, aka the grandparent matrix. The only purpose is to solve
/// issues related to RegisterTile matrix net ID initialization. There are some circumstances where the RegisterTiles are initialized
/// before the NetId of the parent matrix is assigned, so this component ensures that each register tile is informed
/// of the correct parent matrix net ID as soon as it becomes available
/// </summary>
public class GrandparentMatrix : NetworkBehaviour
{
	public override void OnStartServer()
	{
		var myNetId = gameObject.NetId();
		foreach (var rt in GetComponentsInChildren<RegisterTile>())
		{
			rt.ServerSetGrandparentMatrixNetID(myNetId);
		}
	}
}
