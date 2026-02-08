using UnityEngine;
using UnityEngine.UI;
using US13.Core.Addressables;
using US13.Managers;
using US13.UI.Core;

namespace US13.UI
{
	[DisallowMultipleComponent]
	public class ClickyButton : MonoBehaviour
	{


		public void Start()
		{

			this.GetComponent<ToggleButton>()?.onValueChanged?.AddListener(Click);
			this.GetComponent<Button>()?.onClick?.AddListener(Click);

		}

		public void Click()
		{
			Click(true);
		}

		public void Click(bool val)
		{
			_ = SoundManager.Play(CommonSounds.Instance.Click01);
		}
	}
}
