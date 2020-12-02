using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using TMPro;

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
