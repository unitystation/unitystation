using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetManager : SingletonManager<AssetManager>
{
	[SerializeField] private AssetLoadingPopupManager _assetLoadingPopupManager = default;
	private int amountOfAddressablesToLoad = 0;

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
