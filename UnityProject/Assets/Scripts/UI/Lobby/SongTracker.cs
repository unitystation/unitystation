using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class used to display the information of the song being played in the lobby screen.
/// </summary>
public class SongTracker : MonoBehaviour
{
	// Update is called once per frame
	void Update()
	{
		if (!SoundManager.isLobbyMusicPlaying())
			trackPlayingSong(SoundManager.PlayRandomTrack());
	}

	/// <summary>
	/// Adds the name of the song and artist on the text labels.
	/// <param name="songInfo"> string[] that contain the information of the played song</param>
	/// </summary>
	public static void trackPlayingSong(string[] songInfo)
    {
		Transform songInfoLabels = GameObject.Find("SongTracker").transform;
		songInfoLabels.GetChild(1).GetComponent<Text>().text = songInfo[0];
		if (songInfo.Length == 2) // If the name of the artist is included, add it as well
			songInfoLabels.GetChild(2).GetComponent<Text>().text = "By " + songInfo[1];
		else
			songInfoLabels.GetChild(2).GetComponent<Text>().text = "";
	}
}

