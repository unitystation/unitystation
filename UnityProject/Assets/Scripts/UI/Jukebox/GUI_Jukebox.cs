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
	private NetPrefabImage prefabImagePlayStop;

	[SerializeField]
	private NetSlider sliderVolume;

	private Jukebox _JukeboxController;
	private Jukebox jukeboxController
	{
		get
		{
			if (_JukeboxController == null)
				_JukeboxController = Provider.GetComponent<Jukebox>();

			return _JukeboxController;
		}
	}

	public void OnTabOpenedHandler(ConnectedPlayer connectedPlayer)
	{
		labelTrack.Value = jukeboxController.TrackPosition;
		labelSong.Value = jukeboxController.SongName;
		labelArtist.Value = jukeboxController.Artist;
		prefabImagePlayStop.Value = jukeboxController.PlayStopButtonPrefabImage;
	}

	public void PlayOrStop()
    {
		if (jukeboxController.IsPlaying)
			jukeboxController.Stop();
		else
			jukeboxController.Play();
	}

	public void PreviousSong()
	{
		jukeboxController.PreviousSong();
	}

	public void NextSong()
    {
		jukeboxController.NextSong();
	}

	public void ClosePanel()
	{
		ControlTabs.CloseTab(Type, Provider);
	}

	public void VolumeChange()
	{
		jukeboxController.VolumeChange(float.Parse(sliderVolume.Value) / 100);
	}
}
