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

	//public async Task LoadAssetFromReference<T>(AddressableReference<T> reference) where T : UnityEngine.Object
	//{
	//	var loadTask = reference.Load();
	//}

	public void AddLoadingAssetHandle(AsyncOperationHandle handle)
	{
		// pass to asset loading in order to show the player what is currently downloading.
	}

	//public void LoadAssetFromReference<T>(AssetReference reference, System.Action<AsyncOperationHandle<T>> callback)
	//{
	//	Addressables.LoadAssetAsync<T>(reference).Completed += callback;
	//}

	//public void LoadAssetFromPath<T>(string path, System.Action<AsyncOperationHandle<T>> callback)
	//{
	//	Addressables.LoadAssetAsync<T>(path).Completed += callback;
	//}

	//public ConnectToRemoteHostingService(string URL)
	//{

	//}
}
