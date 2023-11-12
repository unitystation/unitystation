using System;
using System.Collections.Generic;
using UnityEngine;
using AddressableReferences;
using Logs;
using Systems.Hacking;

namespace UI.Hacking
{
	public class GUI_Hacking : NetTab
	{
		[SerializeField]
		private AddressableAudioSource Wirecut = null; //TODO

		private HackingProcessBase hackProcess;
		public HackingProcessBase HackProcess => hackProcess;

		public GUI_HackingOutputAndInput Inputs;

		public GUI_HackingOutputAndInput Outputs;

		public GUI_CablePanel GUI_CablePanel;

		private void Start()
		{
			if (Provider != null)
			{
				hackProcess = Provider.GetComponentInChildren<HackingProcessBase>();
				hackProcess.OnChangeServer.AddListener(SetUpData);

				if (CustomNetworkManager.IsServer)
				{
					SetUpData();
				}
			}
		}

		public void SetUpData()
		{
			List<GUI_HackingOutputAndInput.PortData> portToSet = new List<GUI_HackingOutputAndInput.PortData>();

			foreach (var localPortData in hackProcess.PanelInputCurrentPorts)
			{
				var Port = new GUI_HackingOutputAndInput.PortData();
				Port.Colour.SetColour(localPortData.Colour);
				Port.ID = localPortData.LocalID;
				Port.IsInputInToPanel = true;
				portToSet.Add(Port);
			}

			Inputs.Replace(portToSet);
			portToSet = new List<GUI_HackingOutputAndInput.PortData>();

			foreach (var localPortData in hackProcess.PanelOutputCurrentPorts)
			{
				var Port = new GUI_HackingOutputAndInput.PortData();
				Port.Colour.SetColour(localPortData.Colour);
				Port.ID = localPortData.LocalID;
				Port.IsInputInToPanel = false;
				portToSet.Add(Port);
			}

			Outputs.Replace(portToSet);

			List<GUI_CablePanel.CableData> addElements = new List<GUI_CablePanel.CableData>();
			foreach (var cable in hackProcess.Cables)
			{
				GUI_CablePanel.CableData NetCable = new GUI_CablePanel.CableData();
				NetCable.CableNetuID = cable.cableCoilID;
				if (hackProcess.DictionaryCurrentPorts.ContainsKey(cable.PanelOutput))
				{
					NetCable.IDConnectedFrom = hackProcess.DictionaryCurrentPorts[cable.PanelOutput].LocalID;
				}
				else
				{
					Loggy.LogError("Caught KeyNotFound Exception for hackProcess.DictionaryCurrentPorts[cable.PanelOutput] ln 76 GUI_Hacking.cs", Category.Interaction);
					continue;
				}

				if (hackProcess.DictionaryCurrentPorts.ContainsKey(cable.PanelInput))
				{
					NetCable.IDConnectedTo = hackProcess.DictionaryCurrentPorts[cable.PanelInput].LocalID;
				}
				else
				{
					Loggy.LogError("Caught KeyNotFound Exception for hackProcess.DictionaryCurrentPorts[cable.PanelOutput] ln 86 GUI_Hacking.cs", Category.Interaction);
					continue;
				}

				addElements.Add(NetCable);
			}

			GUI_CablePanel.Replace(addElements);
		}

		public void AttemptAddDevice()
		{
			// Pickupable handItem = PlayerManager.LocalPlayerScript.Equipment.ItemStorage.GetActiveHandSlot().Item;
			// if (handItem != null)
			// {
			// MouseInputController.CheckHandApply(hackProcess as IBaseInteractable<HandApply>, hackProcess.gameObject);
			// }
		}

		//Click port  , spawns in cable, Jump to Selected port
		//has pool of unselected cables
	}
}
