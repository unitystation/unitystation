
using Mirror;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// This component lives on the parent transform of Matrix, aka the grandparent matrix. The only purpose is to solve
/// issues related to net ID initialization. There are some circumstances where the other components which depend
/// on this object's net ID are initialized
/// before the NetId of this object is assigned, so this component ensures that each register tile and
/// other dependent objects are informed
/// of the correct parent matrix net ID as soon as it becomes available.
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

	public override void OnStartClient()
	{
		//make sure layer orientations are refreshed now that this matrix is initialized
		foreach (var layer in GetComponentsInChildren<Layer>())
		{
			layer.InitFromMatrix();
		}
	}
}
