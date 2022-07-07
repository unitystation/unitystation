using System.Collections;
using System.Collections.Generic;
using System.Text;
using Detective;
using Mirror;
using UnityEngine;

public class DetectiveScanner : NetworkBehaviour, ICheckedInteractable<PositionalHandApply>
{
	public int ScannerDetail = 5;

	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{

		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (interaction.TargetObject == gameObject) return false;
		if (interaction.IsAltClick)
		{
			return true;
		}

		return false;
	}

	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		AppliedDetails appliedDetails = null;
		string ScanningName = "";


		if (interaction.TargetObject.GetComponent<Attributes>() != null)
		{
			appliedDetails = interaction.TargetObject.GetComponent<Attributes>().OrNull()?.AppliedDetails;
			ScanningName = interaction.TargetObject.ExpensiveName() + " \n Clue ID " + interaction.TargetObject.gameObject.GetInstanceID();
		}
		else
		{
			Vector3 worldPosition = interaction.WorldPositionTarget;
			var matrix = MatrixManager.AtPoint(worldPosition.CutToInt(), true);
			var localPosition = MatrixManager.WorldToLocal(worldPosition, matrix).CutToInt();
			var metaDataNode = matrix.MetaDataLayer.Get(localPosition, false);
			appliedDetails = metaDataNode.AppliedDetails;
			var Tile = matrix.MetaTileMap.GetTile(localPosition);
			if (Tile == null)
			{
				ScanningName = " Nothingness??!? ";
			}
			else
			{
				ScanningName = Tile.name;
			}
		}

		var StringBuilder = new StringBuilder();

		if (appliedDetails == null || appliedDetails.Details.Count == 0)
		{
			StringBuilder.AppendLine($"The scanner Beeps and Boops, Not finding anything on {ScanningName}");
		}
		else
		{
			StringBuilder.AppendLine(
				$"The scanner Beeps and Boops Finding on {ScanningName} ");
			for (int i = 0; i < ScannerDetail; i++)
			{
				if (i == appliedDetails.Details.Count)
				{
					break;
				}

				StringBuilder.AppendLine($" Finding {appliedDetails.Details[i].Description} Clue ID {appliedDetails.Details[i].CausedByInstanceID} ");
			}
		}

		Chat.AddExamineMsgFromServer(interaction.Performer, StringBuilder.ToString());

	}
}
