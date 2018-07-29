using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using WebSocketSharp.Net.WebSockets;

namespace Rcon
{
	public class RconManager : RconConsole
    {
        private static RconManager rconManager;
        public static RconManager Instance
        {
            get
            {
                if (rconManager == null)
                {
                    rconManager = FindObjectOfType<RconManager>();
                }
                return rconManager;
            }
        }

        private HttpServer httpServer;
        private FPSMonitor fpsMonitor;

		private WebSocketServiceHost chatHost;

        private void OnEnable()
        {
            Instance.Init();
        }

        private void OnDisable()
        {
			if (httpServer != null) {
				httpServer.Stop();
			}
        }

        private void Init()
        {
            DontDestroyOnLoad(rconManager.gameObject);
            fpsMonitor = GetComponent<FPSMonitor>();
            StartServer();
        }

        private void StartServer()
        {
            if (httpServer != null)
            {
                Logger.LogWarning("Already Listening: WebSocket");
                return;
            }
            if (!GameData.IsHeadlessServer)
            {
                // Destroy(gameObject);
                // return;
            }

            httpServer = new HttpServer(3005);
            httpServer.AddWebSocketService<RconSocket>("/checkConn");
			httpServer.AddWebSocketService<RconChat>("/rconchat");
            httpServer.Start();

			//Get the service hosts:
			Instance.httpServer.WebSocketServices.TryGetServiceHost("/rconchat", out chatHost);

            if (httpServer.IsListening)
            {
                Logger.Log("Providing websocket services on port " + httpServer.Port);
                foreach (var path in httpServer.WebSocketServices.Paths)
                    Logger.Log("- " + path);
            }
        }

		public static void AddChatLog(string msg){
			Debug.Log("Add CHAT LOG");
			msg = DateTime.UtcNow + ":    " + msg + "<br>";
			AmendChatLog(msg);
			Instance.chatHost.Sessions.Broadcast(msg);
		}

		//Monitoring:
        public  static string GetFPSReadOut()
        {
            return $"FPS Stats: Current: {Instance.fpsMonitor.Current} Average: {Instance.fpsMonitor.Average}" +
                $" GC MEM: {GC.GetTotalMemory(false) / 1024 / 1024} MB";
        }

		public static string GetLastLog(){
			return LastLog;
		}

        public static string GetFullLog()
        {
            return ServerLog;
        }

		public static string GetFullChatLog()
		{
			return ChatLog;
		}


    }

    public class RconSocket : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data == "stats")
            {
                Send(RconManager.GetFPSReadOut());
            }
            if (e.Data == "log")
            {
                Send(RconManager.GetLastLog());
            }
            if(e.Data == "logfull")
            {
                Send(RconManager.GetFullLog());
            }
        }
    }

	public class RconChat : WebSocketBehavior{
		
		protected override void OnOpen()
		{
			Debug.Log("ID: " + ID);
			Debug.Log("Protocol: " + Protocol);
			Debug.Log("Context cookie count: " + Context.CookieCollection.Count);
			Debug.Log("Context Origin: " + Context.Origin);
			Debug.Log("Context Sec websocket key: " + Context.SecWebSocketKey);
			Debug.Log("Context user id name: " + Context.User.Identity.Name);

			base.OnOpen();
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			if (e.Data == "chatfull") {
				Send(RconManager.GetFullChatLog());
			}

			if(e.Data[0] == '1'){
				var msg = e.Data.Substring(1, e.Data.Length);
				ChatEvent chatEvent = new ChatEvent("[Centcomm] " + msg, ChatChannel.OOC);
				ChatRelay.Instance.AddToChatFromRcon(chatEvent);
			}
		}
	}
}
