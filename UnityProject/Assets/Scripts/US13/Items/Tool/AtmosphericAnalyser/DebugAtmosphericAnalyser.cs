using Logs;
using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Managers.MatrixManager;
using US13.Tilemaps.Behaviours.Layers;

namespace US13.Items.Tool.AtmosphericAnalyser
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
			Loggy.Info(toShow);
		}
	}
}
