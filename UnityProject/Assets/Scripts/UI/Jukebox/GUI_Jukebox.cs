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
		//spriteImagePlayStop.SetComplicatedValue(jukeboxController.IsPlaying ? spritePlay.name : spriteStop.name);
	}

	// Start is called before the first frame update
	public void Start()
	{
		spritePlay = buttonPlayStop.transform.Find("SpritePlay").GetComponent<SpriteRenderer>().sprite;
		spriteStop = buttonPlayStop.transform.Find("SpritePlay").GetComponent<SpriteRenderer>().sprite;
		jukeboxController = Provider.GetComponent<Jukebox>();

		SetPlayStopSprite();
	}

	public void PlayOrStop()
    {
		if (jukeboxController.IsPlaying)
			jukeboxController.Stop();
		else
			jukeboxController.Play();

		SetPlayStopSprite();
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
