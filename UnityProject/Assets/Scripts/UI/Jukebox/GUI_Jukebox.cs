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

	private Jukebox jukeboxController;

	public void OnTabOpenedHandler(ConnectedPlayer connectedPlayer)
	{
		labelTrack.Value = jukeboxController.TrackPosition;
		labelSong.Value = jukeboxController.SongName;
		labelArtist.Value = jukeboxController.Artist;
		prefabImagePlayStop.Value = jukeboxController.PlayStopButtonPrefabImage;
	}

	// Start is called before the first frame update
	public void Start()
	{
		jukeboxController = Provider.GetComponent<Jukebox>();
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
}
