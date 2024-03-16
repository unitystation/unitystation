using System;
using DatabaseAPI;
using Discord;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core
{
    public class DiscordClient : MonoBehaviour
    {
	    public Discord.Discord Client { get; private set; } = null;
	    public ActivityManager Activity { get; private set; } = null;

	    private const long CLIENT_ID = 1218638802143416502;

	    private Activity defaultActivity = new Activity
	    {
		    Name = "Unitystation",
		    Type = ActivityType.Playing,
		    Details = "Loading game..",
		    Assets = new ActivityAssets(){ LargeText = "unitystation", LargeImage = "unitystation-logo",
			    SmallImage = "unitystation-logo", SmallText = "unitystation"},
	    };

	    private ActivityTimestamps timestamps = new ActivityTimestamps();

	    private void Start()
	    {
		    Client = new Discord.Discord(CLIENT_ID, (UInt64)Discord.CreateFlags.NoRequireDiscord);
		    Activity = Client.GetActivityManager();
		    timestamps.Start = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		    UpdateActivity(defaultActivity);
	    }

	    private void OnDestroy()
	    {
		    if (Client != null) Client.Dispose();
	    }

	    private void UpdateActivity(Activity activity)
	    {
		    Activity.UpdateActivity(activity, (result) =>
		    {
			    if (result != Discord.Result.Ok)
			    {
				    Debug.LogError("Failed to set activity: " + result);
			    }
			    else
			    {
				    Debug.Log("Activity set successfully!");
			    }
		    });
	    }

	    private void UpdateActivity()
	    {
		    if (MatrixManager.Instance == null || MatrixManager.Instance.ActiveMatricesList.Count == 0 || Activity == null) return;
		    string map = MatrixManager.MainStationMatrix != null ? MatrixManager.MainStationMatrix.SubsystemManager.gameObject.name : "Lobby";
		    string serverName = UIManager.Instance.ServerInfoPanelWindow.MotdPage.ServerName;
		    string details = $"Map: {map}";
		    if (string.IsNullOrEmpty(serverName) == false)
		    {
			    details += $"\n | Server: {serverName}";
		    }
		    Activity newActivity = new Activity()
		    {
			    Name = "Unitystation",
			    Type = ActivityType.Playing,
			    Details = details,
			    Assets = new ActivityAssets(){ LargeText = "unitystation", LargeImage = "unitystation-logo",
				    SmallImage = "unitystation-logo", SmallText = "unitystation"},
			    Timestamps = timestamps,
		    };
		    UpdateActivity(newActivity);
	    }

	    private void Update()
	    {
		    UpdateActivity();
		    Client.RunCallbacks();
	    }
    }
}
