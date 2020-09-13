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
	private static AssetLoadingPopupManager _popupManager;

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
		if (handle.Status == AsyncOperationStatus.Failed || handle.Status == AsyncOperationStatus.Succeeded)
			return;
		_popupManager.AddAssetLoadingPopup(handle, path);
	}
}
