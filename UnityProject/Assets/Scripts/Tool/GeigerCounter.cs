using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeigerCounter : MonoBehaviour, IInteractable<HandActivate>
{


	public void ServerPerformInteraction(HandActivate interaction)
	{
		Vector3Int worldPosInt = interaction.Performer.GetComponent<PlayerScript>().registerTile.WorldPosition;
		MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
		var matrix = interaction.Performer.GetComponentInParent<Matrix>();
		var MetaDataNode = matrix.GetMetaDataNode(localPosInt);
		Chat.AddExamineMsgFromServer(interaction.Performer,
			" The Geiger counter reads " + MetaDataNode.RadiationNode.RadiationLevel);
	}

}
