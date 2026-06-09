using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using US13.Managers;
using US13.Managers.UpdateManager;

namespace US13.Core.Addressables
{
	public class AssetLoadingPopup : MonoBehaviour
	{
		[SerializeField] private TextMeshProUGUI textUGUI = default;
		[SerializeField] private Slider loadingSlider = default;
		AsyncOperationHandle handle;

		public void Setup(AsyncOperationHandle asyncOperationHandle, string path)
		{
			textUGUI.text = path;
			handle = asyncOperationHandle;
		}

		private void OnEnable()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		private void UpdateMe()
		{
			if (handle.IsDone)
			{
				AssetManager.Instance.AssetDoneLoading();
				gameObject.SetActive(false);
			}
			loadingSlider.value = handle.PercentComplete;
		}
	}
}
