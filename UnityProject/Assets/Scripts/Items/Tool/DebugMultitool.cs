using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Systems.Electricity;

namespace Items.Engineering
{
	public class DebugMultitool : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
	{
		public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
		{
			if (interaction.HandObject == null) return false;
			return true;
		}

		public void ServerPerformInteraction(PositionalHandApply interaction)
		{
			Vector3Int worldPosInt = interaction.WorldPositionTarget.To2Int().To3Int();
			MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
			var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
			var matrix = interaction.Performer.GetComponentInParent<Matrix>();
			var MetaDataNode = matrix.GetElectricalConnections(localPosInt);
			StringBuilder SB = new StringBuilder();
			SB.Append("MetaDataNodeCount " + MetaDataNode.Count);
			SB.Append("\n");

			foreach (var D in MetaDataNode)
			{
				SB.Append(D.ShowDetails());
			}
			MetaDataNode.Clear();
			ElectricalPool.PooledFPCList.Add(MetaDataNode);
			Chat.AddExamineMsgFromServer(interaction.Performer, SB.ToString());
			Logger.Log(SB.ToString());
		}
	}
}
