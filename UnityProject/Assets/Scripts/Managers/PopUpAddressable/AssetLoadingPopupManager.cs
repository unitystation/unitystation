using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetLoadingPopupManager : MonoBehaviour
{
	[SerializeField] private GameObject popupPrefab = default;
	[SerializeField] private RectTransform popupHolder = default;

	public void AddAssetLoadingPopup(AsyncOperationHandle handle, string path)
	{
		var popup = Instantiate(popupPrefab, popupHolder);
		popup.GetComponent<AssetLoadingPopup>().Setup(handle, path);
	}
}
