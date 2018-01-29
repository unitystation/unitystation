using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using Facepunch.Steamworks;
using UnityEngine;
using UnityEngine.Rendering;

//
// This class takes care of a lot of stuff for you.
//
//  1. It initializes steam on startup.
//  2. It calls Update so you don't have to.
//  3. It disposes and shuts down Steam on close.
//
// You don't need to reference this class anywhere or access the client via it.
// To access the client use Facepunch.Steamworks.Client.Instance, see SteamAvatar
// for an example of doing this in a nice way.
//
public class SteamClient : MonoBehaviour
{
    public uint AppId;

    private Facepunch.Steamworks.Client client;

    void Start()
    {
        // keep us around until the game closes
        GameObject.DontDestroyOnLoad(gameObject);
        // We do not want a client running on a dedicated server
 
            if (AppId == 0)
                throw new System.Exception("You need to set the AppId to your game");

            //
            // Configure us for this unity platform
            //
            Facepunch.Steamworks.Config.ForUnity(Application.platform.ToString());

            //
            // Create a steam_appid.txt (this seems greasy as fuck, but this is exactly
            // what UE's Steamworks plugin does, so fuck it.
            //
            try
            {
                System.IO.File.WriteAllText("steam_appid.txt", AppId.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Couldn't write steam_appid.txt: " + e.Message);
            }

            // Create the client
        if (GameData.IsHeadlessServer || GameData.Instance.testServer || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Debug.Log("Skipping Steam Client Init as this is a Headless Server");
        }
        else
        {
            client = new Facepunch.Steamworks.Client(AppId);

            // Prevents NRE's if something goes wrong with the Client
            if (client != null)
            {
                if (!client.IsValid)
                {
                    client = null;
                    Debug.LogWarning("Couldn't initialize Steam");
                    return;
                }

                Debug.Log("Steam Initialized: " + client.Username + " / " + client.SteamId);
            }
        }
    }

    void Update()
    {
        // Makes sure the steam client gets updated
        if (client == null)
            return;

        try
        {
            UnityEngine.Profiling.Profiler.BeginSample("Steam Update");
            client.Update();
        }
        finally
        {
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    private void OnDestroy()
    {
        // disposes the steamclient when the steammanager/steamclient-script is destroyed
        if (client != null)
        {
            Client.Instance.Auth.GetAuthSessionTicket().Cancel();
            client.Dispose();
            client = null;
        }

    }

}
