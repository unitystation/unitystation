using System.Collections.Generic;
using Logs;
using UnityEngine;


namespace Items.Atmospherics
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
			Vector3Int worldPosInt = interaction.WorldPositionTarget.RoundTo2Int().To3Int();
			MatrixInfo matrixinfo = MatrixManager.AtPoint(worldPosInt, true);
			var localPosInt = MatrixManager.WorldToLocalInt(worldPosInt, matrixinfo);
			var matrix = interaction.Performer.GetComponentInParent<Matrix>();

			string toShow = "";
			foreach (var pipeNode in matrix.GetPipeConnections(localPosInt))
			{
				toShow += pipeNode.ToString() + "\n";
			}

			Chat.AddExamineMsgFromServer(interaction.Performer, toShow);
			Loggy.Log(toShow);
		}
	}
}
