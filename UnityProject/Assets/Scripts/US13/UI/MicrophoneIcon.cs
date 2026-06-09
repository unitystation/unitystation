using Shared.Managers;

namespace US13.UI
{
	public class MicrophoneIcon : SingletonManager<MicrophoneIcon>
	{

		public override void Start()
		{
			base.Start();
			this.gameObject.SetActive(false);
		}

	}
}
