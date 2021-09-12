using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using NetElements;
using UnityEngine;

namespace Hacking
{
	public class GUI_CablePanel : NetListButBetter<GUI_CablePanel.CableData>
	{
		public class CableData
		{
			public int IDConnectedTo = -1;
			public int IDConnectedFrom = -1;
			public uint CableNetuID = 0;
		}

		public GUI_Hacking GUI_Hacking;

		public GUI_HackingWire WirePrefab;

		public Transform CableLayer;

		public List<GUI_HackingWire> OpenCables = new List<GUI_HackingWire>();

		public override void ElementsChanged(List<CableData> NewList, List<CableData> OldList)
		{
			List<CableData> remove = new List<CableData>();
			List<CableData> add = new List<CableData>();
			foreach (var newOne in NewList)
			{
				bool Has = false;
				foreach (var Oldone in OldList)
				{
					if (newOne.CableNetuID == Oldone.CableNetuID)
					{
						Has = true;
					}
				}

				if (Has == false)
				{
					add.Add(newOne);
				}
			}

			foreach (var oldOne in OldList)
			{
				bool Has = false;
				foreach (var Newone in NewList)
				{
					if (Newone.CableNetuID == oldOne.CableNetuID)
					{
						Has = true;
					}
				}

				if (Has == false)
				{
					remove.Add(oldOne);
				}
			}


			foreach (var CD in add)
			{
				GUI_Hacking.Inputs.SelectedHackingPort.OrNull()?.UnSelect();
				GUI_Hacking.Outputs.SelectedHackingPort.OrNull()?.UnSelect();
				var Cable = Instantiate(WirePrefab, CableLayer.transform);
				Cable.SetUp(CD, this);
				OpenCables.Add(Cable);

			}

			foreach (var PD in remove)
			{
				GUI_HackingWire GUIHackingWire = null;
				foreach (var GUI in OpenCables)
				{
					if (GUI.ThisCableData.CableNetuID == PD.CableNetuID)
					{
						GUIHackingWire = GUI;
						break;
					}
				}

				OpenCables.Remove(GUIHackingWire);
				Destroy(GUIHackingWire.gameObject);
			}
		}

		public void CheckSelected()
		{
			if (GUI_Hacking.Inputs.SelectedHackingPort != null && GUI_Hacking.Outputs.SelectedHackingPort != null)
			{
				RequestHackingInteraction.Send(GUI_Hacking.HackProcess.gameObject,
					PlayerManager.LocalPlayerScript.Equipment.ItemStorage.GetActiveHandSlot().Item.netId,
					GUI_Hacking.Inputs.SelectedHackingPort.PortData.ID,
					GUI_Hacking.Outputs.SelectedHackingPort.PortData.ID,
					RequestHackingInteraction.InteractionWith.Cable);
			}
		}
	}
}