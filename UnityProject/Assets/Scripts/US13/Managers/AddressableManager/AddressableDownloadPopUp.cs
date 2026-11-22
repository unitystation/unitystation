using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
namespace US13.Managers.AddressableManager
{
	public class AddressableDownloadPopUp : MonoBehaviour
	{
		public AddressableBar DownloadBar;
		public AddressableBar Track(AsyncOperationHandle handle, string name)
		{
			if (DownloadBar == null) return null;
			var bar = Instantiate(DownloadBar, transform);
			bar.Setup(handle, name);
			return bar;
		}
	}
}
