using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
namespace US13.Managers.AddressableManager
{
	public class AddressableBar : MonoBehaviour
	{
		public Slider slider;
		public AsyncOperationHandle SetHandle;
		public TMP_Text Text;
		public void Setup(AsyncOperationHandle handle, string name)
		{
			SetHandle = handle;
			Text.text = "Downloading " + name;
		}
		private void Update()
		{
			if (!SetHandle.IsValid()) return;
			var status = SetHandle.GetDownloadStatus();
			slider.value = status.TotalBytes > 0 ? (float)status.DownloadedBytes / status.TotalBytes : SetHandle.PercentComplete;
		}
	}
}
