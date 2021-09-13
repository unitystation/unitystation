using System.Collections;
using System.Collections.Generic;
using NetElements;
using UnityEngine;

namespace Hacking
{
	public class GUI_HackingOutputAndInput : NetListButBetter<GUI_HackingOutputAndInput.PortData>
	{
		public GUI_Hacking GUI_Hacking;
		public GameObject parent;

		public GUI_HackingPort Prefab;

		public List<GUI_HackingPort> OpenPorts = new List<GUI_HackingPort>();

		public Dictionary<int, GUI_HackingPort> IDtoPort = new Dictionary<int, GUI_HackingPort>();

		public GUI_HackingPort SelectedHackingPort;

		public class PortData
		{
			public NetFriendlyColour Colour = new NetFriendlyColour();
			public bool IsInputInToPanel;
			public int ID;
		}

		public override void ElementsChanged(List<PortData> NewList, List<PortData> OldList)
		{
			List<PortData> remove = new List<PortData>();
			List<PortData> add = new List<PortData>();
			foreach (var newOne in NewList)
			{
				bool Has = false;
				foreach (var Oldone in OldList)
				{
					if (newOne.ID == Oldone.ID)
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
					if (Newone.ID == oldOne.ID)
					{
						Has = true;
					}
				}

				if (Has == false)
				{
					remove.Add(oldOne);
				}
			}


			foreach (var PD in add)
			{
				var Port = Instantiate(Prefab, parent.transform);
				Port.SetUp(PD, this);
				OpenPorts.Add(Port);
				IDtoPort[Port.PortData.ID] = Port;
			}

			foreach (var PD in remove)
			{
				GUI_HackingPort GUIHackingPort = null;
				foreach (var GUI in OpenPorts)
				{
					if (GUI.PortData.ID == PD.ID)
					{
						GUIHackingPort = GUI;
						break;
					}
				}

				OpenPorts.Remove(GUIHackingPort);
				IDtoPort.Remove(GUIHackingPort.PortData.ID);
				Destroy(GUIHackingPort.gameObject);
			}
		}


		public void SetPortSelected(GUI_HackingPort inSelectedHackingPort)
		{
			if (SelectedHackingPort != null)
			{
				SelectedHackingPort.UnSelect(false);
			}

			SelectedHackingPort = inSelectedHackingPort;
			GUI_Hacking.GUI_CablePanel.CheckSelected();
		}
	}
}