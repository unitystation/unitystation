using System.Collections;
using System.Collections.Generic;
using DatabaseAPI;
using UnityEngine;
using TMPro;
using Mirror;
using System.IO;
using System;
using Initialisation;
using Managers;
using Messages.Client;
using Messages.Server;
using UI;
using UI.Systems.PreRound;

namespace ServerInfo
{
	public class ServerInfoUILobby : MonoBehaviour, IInitialise
    {
    	public TMP_Text ServerName;

    	public TMP_Text ServerDesc;

    	public TMP_Text ServerRules;

        public GameObject ServerInfoUILobbyObject;

        public GameObject DiscordButton;

        public static string serverDesc;

        public static string serverDiscordID;

        public static string serverRules;

        public InitialisationSystems Subsystem => InitialisationSystems.ServerInfoUILobby;

        [SerializeField] private Transform tabButtons;
        [SerializeField] private Transform eventButton;
        [SerializeField] private Transform ruleButton;
        [SerializeField] private Transform pageMOTD;
        [SerializeField] private Transform pageRules;
        [SerializeField] private Transform pageEvents;
        [SerializeField] private GameObject eventEntry;

        private List<EventEntry> entries = new List<EventEntry>();

        void IInitialise.Initialise()
        {
	        LoadNameAndDesc();
	        LoadLinks();
        }

        private void LoadNameAndDesc()
        {
	        var pathDesc = Path.Combine(Application.streamingAssetsPath, "config", "serverDesc.txt");
	        var pathRules = Path.Combine(Application.streamingAssetsPath, "config", "serverRules.txt");

	        var descText = "";
	        var rulesText = "";

	        if (File.Exists(pathDesc))
	        {
		        descText = File.ReadAllText(pathDesc);
	        }
	        if (File.Exists(pathRules))
	        {
		        rulesText = File.ReadAllText(pathRules);
	        }

	        var nameText = ServerData.ServerConfig.ServerName;

	        ServerName.text = nameText;
	        ServerDesc.text = descText;
	        ServerRules.text = rulesText;
	        serverDesc = descText;
        }

        private void LoadLinks()
        {
	        if (string.IsNullOrEmpty(ServerData.ServerConfig.DiscordLinkID)) return;

	        serverDiscordID = ServerData.ServerConfig.DiscordLinkID;
        }

        public void ClientSetValues(string newName, string newDesc, string newDiscordID, string rules)
        {
	        ServerName.text = newName;
	        ServerDesc.text = newDesc;
	        serverDiscordID = newDiscordID;
	        ServerRules.text = rules;


	        if (string.IsNullOrEmpty(newDesc) == false) ServerInfoUILobbyObject.SetActive(true);
	        if (string.IsNullOrEmpty(rules)) ruleButton.SetActive(false);
	        if (CheckIfTheresAnEventOnStarting() == false) eventButton.SetActive(false);
	        if (ruleButton.gameObject.activeSelf == false && eventButton.gameObject.activeSelf == false) tabButtons.SetActive(false);
	        if (string.IsNullOrEmpty(newDiscordID)) return;
	        DiscordButton.SetActive(true);
	        DiscordButton.GetComponent<OpenURL>().url = "https://discord.gg/" + newDiscordID;
        }

        private bool CheckIfTheresAnEventOnStarting()
        {
	        var result = TimedEventsManager.Instance.ActiveEvents.Count > 0;
	        if(result) UpdateEventEntryList();
	        return result;
        }

        private void UpdateEventEntryList()
        {
	        foreach (var entry in entries)
	        {
		        Destroy(entry.gameObject);
	        }
	        entries.Clear();

	        foreach (var eventSo in TimedEventsManager.Instance.ActiveEvents)
	        {
		        GameObject entry = Instantiate(eventEntry);//creates new button
		        entry.SetActive(true);
		        var c = entry.GetComponent<EventEntry>();
		        c.EventImage.sprite = eventSo.EventIcon;
		        c.EventName.text = eventSo.EventName;
		        c.EventDesc.text = eventSo.EventDesc;
		        entries.Add(c);
		        entry.transform.SetParent(pageEvents, false);
	        }
        }

        public void OnPageRulesClick()
        {
	        HideAllPages();
	        pageRules.SetActive(true);
        }

        public void OnPageMotdClick()
        {
	        HideAllPages();
	        pageMOTD.SetActive(true);
        }

        public void OnPageEventsClick()
        {
	        HideAllPages();
	        pageEvents.SetActive(true);
        }

        private void HideAllPages()
        {
	        pageMOTD.SetActive(false);
	        pageRules.SetActive(false);
	        pageEvents.SetActive(false);
        }

        [Serializable]
        public class ServerInfoLinks
        {
	        public string DiscordLinkID;
        }
    }

	public class ServerInfoLobbyMessageServer : ServerMessage<ServerInfoLobbyMessageServer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string ServerName;
			public string ServerDesc;
			public string ServerRules;
			public string ServerDiscordID;
		}

		public override void Process(NetMessage msg)
		{
			GUI_PreRoundWindow.Instance.GetComponent<ServerInfoUILobby>().ClientSetValues(
					msg.ServerName, msg.ServerDesc, msg.ServerDiscordID, msg.ServerRules);
		}

		public static NetMessage Send(NetworkConnection clientConn,string serverName, string serverDesc, string serverDiscordID, string serverRules)
		{
			NetMessage msg = new NetMessage
			{
				ServerName = serverName,
				ServerDesc = serverDesc,
				ServerRules = serverRules,
				ServerDiscordID = serverDiscordID
			};

			SendTo(clientConn, msg);
			return msg;
		}
	}

	public class ServerInfoLobbyMessageClient : ClientMessage<ServerInfoLobbyMessageClient.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			ServerInfoLobbyMessageServer.Send(
					SentByPlayer.Connection, ServerData.ServerConfig.ServerName,
					ServerInfoUILobby.serverDesc, ServerInfoUILobby.serverDiscordID, ServerInfoUILobby.serverRules);
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();

			Send(msg);
			return msg;
		}
	}
}
