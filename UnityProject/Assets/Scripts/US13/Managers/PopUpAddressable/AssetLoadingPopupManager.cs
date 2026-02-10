using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using US13.Core.Addressables;

namespace US13.Managers.PopUpAddressable
{
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
}
