using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Rcon
{
    public class RconManager : MonoBehaviour
    {
        private static RconManager rconManager;
        public static RconManager Instance
        {
            get
            {
                if(rconManager == null)
                {
                    rconManager = FindObjectOfType<RconManager>();
                }
                return rconManager;
            }
        }

        private HttpServer httpServer;

        private void OnEnable()
        {
            Instance.Init();
        }

        private void Init()
        {
            DontDestroyOnLoad(rconManager.gameObject);
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
    }
}

public class RconSocket : WebSocketBehavior
{
    protected override void OnMessage(MessageEventArgs e)
    {
        var name = Context.QueryString["name"];
        Send(!name.IsNullOrEmpty() ? String.Format("\"{0}\" to {1}", e.Data, name) : e.Data);
    }
}
