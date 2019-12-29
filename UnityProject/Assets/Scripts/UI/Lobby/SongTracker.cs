using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class used to display the information of the song being played in the lobby screen.
/// </summary>
public class SongTracker : MonoBehaviour
{
	[SerializeField] private Text trackName;
	[SerializeField] private Text artist;

	private float timeBetweenSongs = 2f;
	private float currentWaitTime = 0f;

	/// <summary>
	/// If true the SongTracker will continue to play tracks one after
	/// another in a random order
	/// </summary>
	public bool PlayingRandomPlayList { get; private set; }

	void Awake()
	{
		ToggleUI(false);
	}

	void Update()
	{
		if (!PlayingRandomPlayList) return;

		if (!SoundManager.isLobbyMusicPlaying())
		{
			currentWaitTime += Time.deltaTime;
			if (currentWaitTime >= timeBetweenSongs)
			{
				currentWaitTime = 0f;
				PlayRandomTrack();
			}
		}
	}

	public void StartPlayingRandomPlaylist()
	{
		PlayingRandomPlayList = true;
		PlayRandomTrack();
		ToggleUI(true);
	}

	public void Stop()
	{
		PlayingRandomPlayList = false;
		ToggleUI(false);
		SoundManager.StopMusic();
	}

	void ToggleUI(bool isActive)
	{
		trackName.gameObject.SetActive(isActive);
		artist.gameObject.SetActive(isActive);
	}

	void PlayRandomTrack()
	{
		var songInfo = SoundManager.PlayRandomTrack();
		trackName.text = songInfo[0];
		// If the name of the artist is included, add it as well
		if (songInfo.Length == 2)
		{
			artist.text = songInfo[1];
		}
		else
		{
			artist.text = "";
		}
	}
}