using UnityEngine;
using US13.Core.Addressables;
using US13.Managers;

namespace US13.UI.Systems.Lobby
{
	public class GUI_HornHonk : MonoBehaviour
	{
		public void Hornhonk()
		{
			_ = SoundManager.Play(CommonSounds.Instance.ClownHonk);
		}
	}
}
