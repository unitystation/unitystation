using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	void Start ()
    {
        // keep us around until the game closes
        GameObject.DontDestroyOnLoad(gameObject);

        if (AppId == 0)
            throw new System.Exception("You need to set the AppId to your game");

        //
        // Configure us for this unity platform
        //
        Facepunch.Steamworks.Config.ForUnity( Application.platform.ToString() );

        //
        // Create a steam_appid.txt (this seems greasy as fuck, but this is exactly
        // what UE's Steamworks plugin does, so fuck it.
        //
        try
        {
            System.IO.File.WriteAllText("steam_appid.txt", AppId.ToString());
        }
        catch ( System.Exception e )
        {
            Debug.LogWarning("Couldn't write steam_appid.txt: " + e.Message );
        }

        // Create the client
        client = new Facepunch.Steamworks.Client( AppId );

        if ( !client.IsValid )
        {
            client = null;
            Debug.LogWarning("Couldn't initialize Steam");
            return;
        }

        Debug.Log( "Steam Initialized: " + client.Username + " / " + client.SteamId ); 
	}
	
	void Update()
    {
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
        if (client != null)
        {
            client.Dispose();
            client = null;
        }
    }
}
