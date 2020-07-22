using UnityEngine;
using UnityEngine.UI;

public class GUI_Jukebox : NetTab
{
	[SerializeField]
	private NetLabel labelSong;

	[SerializeField]
	private NetLabel labelArtist;

	[SerializeField]
	private NetLabel labelTrack;

	[SerializeField]
	private NetButton buttonPlayStop;

	[SerializeField]
	private NetSpriteImage spriteImagePlayStop;

	private Jukebox jukeboxController;

	private Sprite spritePlay;
	private Sprite spriteStop;

	private void SetPlayStopSprite()
	{
		spriteImagePlayStop.SetValueServer(jukeboxController.IsPlaying ? spritePlay.name : spriteStop.name);
	}

	private void SetSongAndArtist()
	{
		labelTrack.SetValueServer($"Track {jukeboxController.CurrentTrackIndex + 1} / {jukeboxController.TotalTrackCount}");

		string songName = jukeboxController.CurrentSong;
		labelSong.SetValueServer($"Song : {songName.Split('_')[0]}");
		string artist = songName.Contains("_") ? songName.Split('_')[1] : "Unknown";
		labelArtist.SetValueServer($"Artist : {artist}");
	}

	// Start is called before the first frame update
	public void Start()
	{
		spritePlay = buttonPlayStop.transform.Find("SpritePlay").GetComponent<SpriteRenderer>().sprite;
		spriteStop = buttonPlayStop.transform.Find("SpritePlay").GetComponent<SpriteRenderer>().sprite;
		jukeboxController = Provider.GetComponent<Jukebox>();

		SetSongAndArtist();
		SetPlayStopSprite();
	}

	public void PlayOrStop()
    {
		if (jukeboxController.IsPlaying)
			jukeboxController.Stop();
		else
			jukeboxController.Play();

		SetPlayStopSprite();
		SetSongAndArtist();
	}

	public void PreviousSong()
	{
		jukeboxController.PreviousSong();
		SetSongAndArtist();
	}

	public void NextSong()
    {
		jukeboxController.NextSong();
		SetSongAndArtist();
	}

	public void ClosePanel()
	{
		ControlTabs.CloseTab(Type, Provider);
	}
}
