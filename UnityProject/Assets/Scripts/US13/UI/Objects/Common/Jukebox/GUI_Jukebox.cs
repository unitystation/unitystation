using UnityEngine;
using US13.Managers;
using US13.UI.Core;
using US13.UI.Core.Net;
using US13.UI.Core.Net.Elements;

namespace US13.UI.Objects.Common.Jukebox
{
	public class GUI_Jukebox : NetTab
	{
		[SerializeField]
		private NetText_label labelSong = null;

		[SerializeField]
		private NetText_label labelArtist = null;

		[SerializeField]
		private NetText_label labelTrack = null;

		[SerializeField]
		private NetPrefabImage prefabImagePlayStop = null;

		[SerializeField]
		private NetSlider sliderVolume = null;

		private US13.Objects.Machines.Jukebox jukebox;
		private US13.Objects.Machines.Jukebox Jukebox => jukebox ??= Provider.GetComponent<US13.Objects.Machines.Jukebox>();

		public void OnTabOpenedHandler(PlayerInfo connectedPlayer)
		{
			labelTrack.MasterSetValue(Jukebox.TrackPosition);
			labelSong.MasterSetValue(Jukebox.SongName) ;
			labelArtist.MasterSetValue(Jukebox.Artist);
			prefabImagePlayStop.MasterSetValue(Jukebox.PlayStopButtonPrefabImage);
		}

		public void PlayOrStop()
		{
			if (Jukebox.IsPlaying)
			{
				Jukebox.Stop();
			}
			else
			{
				_ = Jukebox.Play();
			}
		}

		public void PreviousSong()
		{
			Jukebox.PreviousSong();
		}

		public void NextSong()
		{
			Jukebox.NextSong();
		}

		public void ClosePanel()
		{
			ControlTabs.CloseTab(Type, Provider);
		}

		public void VolumeChange()
		{
			Jukebox.VolumeChange(float.Parse(sliderVolume.Value) / 100);
		}
	}
}
