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
}