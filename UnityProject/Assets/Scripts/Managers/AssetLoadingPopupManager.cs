using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetLoadingPopupManager : MonoBehaviour
{
	[SerializeField] private GameObject popupPrefab;
	[SerializeField] private RectTransform popupHolder;

	public void AddAssetLoadingPopup(AsyncOperationHandle handle, string path)
	{
		var popup = Instantiate(popupPrefab, popupHolder);
		popup.GetComponent<AssetLoadingPopup>().Setup(handle, path);
	}
}
