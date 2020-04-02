using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireCutter : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		// If there's a table, we should drop there
		if (MatrixManager.IsTableAt(interaction.WorldPositionTarget.RoundToInt(), side == NetworkSide.Server))
		{
			return false;
		}

		return true;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
		MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
		var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
		var matrix = interaction.Performer.GetComponentInParent<Matrix>();
		var MetaDataNode = matrix.GetMetaDataNode(localPosInt);

		if (MetaDataNode.ElectricalData.Count > 0) {
			//matrix
			Logger.Log(MetaDataNode.ElectricalData[0].InData.Categorytype.ToString());
			matrix.RemoveUnderFloorTile(MetaDataNode.ElectricalData[0].NodeLocation, MetaDataNode.ElectricalData[0].RelatedTile);
			MetaDataNode.ElectricalData[0].InData.DestroyThisPlease();
		}
		Logger.Log("end");
	}


}
