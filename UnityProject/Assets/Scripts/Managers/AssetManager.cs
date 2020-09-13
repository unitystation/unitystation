using AddressableReferences;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetManager : MonoBehaviour
{
	private static AssetManager _assetManager;
	private static AssetLoadingPopupManager _assetLoadingPopupManager;
	private int amountOfAddressablesToLoad = 0;

	public static AssetManager Instance
	{
		get
		{
			if (_assetManager == null)
			{
				_assetManager = FindObjectOfType<AssetManager>();
			}

			return _assetManager;
		}
	}

	public void AddLoadingAssetHandle(AsyncOperationHandle handle, string path)
	{
		if (!_assetLoadingPopupManager.gameObject.activeSelf)
			_assetLoadingPopupManager.gameObject.SetActive(true);

		amountOfAddressablesToLoad++;

		if (handle.Status == AsyncOperationStatus.Failed || handle.Status == AsyncOperationStatus.Succeeded)
			return;
		_assetLoadingPopupManager.AddAssetLoadingPopup(handle, path);
	}

	public void AssetDoneLoading()
	{
		amountOfAddressablesToLoad--;
		if (amountOfAddressablesToLoad <= 0)
			_assetLoadingPopupManager.gameObject.SetActive(false);
	}
}
