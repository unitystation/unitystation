using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DatabaseAPI
{
    /// <summary>
    /// If activated on a server then it will update the
    /// unitystation rest api with the status of this server
    /// To gain access to the unitystation hub for your server
    /// speak to unitystation staff on discord
    /// </summary>
    public partial class ServerData
    {
        private ServerConfig config;
        /// <summary>
        /// The server config that holds the values 
        /// for your RCON and Unitystation HUB API connections
        /// </summary>
        public static ServerConfig ServerConfig
        {
            get
            {
                return Instance.config;
            }
        }

        private bool connectedToHub = false;
        private string hubCookie;
        private const string hubRoot = "https://api.unitystation.org";
        private const string hubLogin = hubRoot + "/login?data=";

        void AttemptConfigLoad()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "/config/config.json");
            if (File.Exists(path))
            {
                config = JsonUtility.FromJson<ServerConfig>(File.ReadAllText(path));
                AttemptHubConnection();
            }
            else
            {
                Logger.Log("No config found for Rcon and Server Hub connections", Category.DatabaseAPI);
            }
        }

        //Attempts to auth with api.unitystation.org for server status updates
        void AttemptHubConnection()
        {
            if (string.IsNullOrEmpty(config.HubUser) || string.IsNullOrEmpty(config.HubPass))
            {
                Logger.Log("Invalid Hub creds found, aborting HUB connection", Category.DatabaseAPI);
                return;
            }

            var loginReq = new HubLoginReq
            {
                username = config.HubUser,
                password = config.HubPass
            };

            Instance.StartCoroutine(Instance.TryHubLogin(loginReq));
        }

        IEnumerator TryHubLogin(HubLoginReq loginRequest)
        {
            var requestData = JsonUtility.ToJson(loginRequest);
            UnityWebRequest r = UnityWebRequest.Get(hubLogin + UnityWebRequest.EscapeURL(requestData));
            yield return r.SendWebRequest();
            if (r.error != null)
            {
                Logger.Log("Hub Login request failed: " + r.error, Category.DatabaseAPI);
                yield break;
            }
            else
            {
                string s = r.GetResponseHeader("set-cookie");
                hubCookie = s.Split(';') [0];
                Logger.Log("Hub connected successfully", Category.DatabaseAPI);
                connectedToHub = true;
            }
        }
    }

    [Serializable]
    public class HubLoginReq
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class ServerStatus
    {
        public string ServerName;
        public string ForkName;
        public int BuildVersion;
        public string CurrentMap;
        public string GameMode;
        public string IngameTime;
        public int PlayerCount;
        public string ServerIP;
        public int ServerPort;
        public string WinDownload;
        public string OSXDownload;
        public string LinuxDownload;
    }

    //Read from Streaming Assets/config/config.json on the server
    [Serializable]
    public class ServerConfig
    {
        public string RconPass;
        public int RconPort;
        //CertKey needed in the future for SSL Rcon
        public string certKey;
        public string HubUser;
        public string HubPass;
        public string ServerName;
        //Location on the internet where clients can be downloaded from:
        public string WinDownload;
        public string OSXDownload;
        public string LinuxDownload;
    }

    //Used to identify the build and fork of this client/server
    [Serializable]
    public class BuildInfo
    {
        //This is used in the HUB to determine if the player has the right
        //build for your server
        public int BuildNumber;
        //I.E. Unitystation, ColonialMarines, BeeStation
        public string ForkName;
    }
}