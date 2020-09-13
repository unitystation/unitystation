using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class AssetLoadingPopup : MonoBehaviour
{
	[SerializeField] private TMPro.TextMeshProUGUI textUGUI;
	[SerializeField] private Slider loadingSlider;
	AsyncOperationHandle handle;
	public void Setup(AsyncOperationHandle asyncOperationHandle, string path)
	{
		textUGUI.text = path;
		handle = asyncOperationHandle;
	}

	private void Update()
	{
		if (handle.IsDone)
		{
			AssetManager.Instance.AssetDoneLoading();
			Destroy(gameObject);
		}

		loadingSlider.value = handle.PercentComplete;
	}
}
