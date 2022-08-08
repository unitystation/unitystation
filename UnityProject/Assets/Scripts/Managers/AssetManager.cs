using Shared.Util;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util;

public class AssetManager : MonoBehaviour
{
	[SerializeField] private AssetLoadingPopupManager _assetLoadingPopupManager = default;
	private static AssetManager _assetManager;
	private int amountOfAddressablesToLoad = 0;

	public static AssetManager Instance => FindUtils.LazyFindObject(ref _assetManager);

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
