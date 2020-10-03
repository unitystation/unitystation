using UnityEngine;
using UnityEngine.UI;

public class GUI_Jukebox : NetTab
{
	[SerializeField]
	private NetLabel labelSong = null;

	[SerializeField]
	private NetLabel labelArtist = null;

	[SerializeField]
	private NetLabel labelTrack = null;

	[SerializeField]
	private NetPrefabImage prefabImagePlayStop = null;

	[SerializeField]
	private NetSlider sliderVolume = null;

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
