using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

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

        private void OnEnable()
        {
            Instance.Init();
        }

        private void OnDisable()
        {
            httpServer.Stop();
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
            httpServer.Start();

            if (httpServer.IsListening)
            {
                Logger.Log("Providing websocket services on port " + httpServer.Port);
                foreach (var path in httpServer.WebSocketServices.Paths)
                    Logger.Log("- " + path);
            }
        }

        public string GetFPSReadOut()
        {
            return $"FPS Stats: Current: {fpsMonitor.Current} Average: {fpsMonitor.Average}" +
                $" GC MEM: {GC.GetTotalMemory(false) / 1024 / 1024} MB";
        }

		public string GetLastLog(){
			return LastLog;
		}
    }

    public class RconSocket : WebSocketBehavior
    {
        private string lastLog;
        protected override void OnMessage(MessageEventArgs e)
        {
            if (e.Data == "stats")
            {
                Send(RconManager.Instance.GetFPSReadOut());
            }
            if (e.Data == "log")
            {
                Send(RconManager.Instance.GetLastLog());
            }
        }
    }
}
