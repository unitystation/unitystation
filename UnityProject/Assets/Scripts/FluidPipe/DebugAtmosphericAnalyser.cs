using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pipes
{
	public class DebugAtmosphericAnalyser : MonoBehaviour, ICheckedInteractable<PositionalHandApply>
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
			var MetaDataNode = matrix.GetMetaDataNode(localPosInt);

			string toShow = "";
			foreach (var pipeNode in matrix.GetPipeConnections(localPosInt))
			{
				toShow += pipeNode.ToString() + "\n";
			}


			Chat.AddExamineMsgFromServer(interaction.Performer, toShow);
		}
	}
}