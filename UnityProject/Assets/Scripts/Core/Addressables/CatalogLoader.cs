using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class CatalogLoader : MonoBehaviour
{
    public List<IResourceLocation> locations;

    public void LoadCatalog(string catalogURL)
    {
        Logger.Log("loadCatalog");
        AsyncOperationHandle<IResourceLocator> handle = Addressables.LoadContentCatalogAsync(catalogURL);
        handle.Completed += loadCatalogsCompleted;
    }
    void loadCatalogsCompleted(AsyncOperationHandle<IResourceLocator> obj)
    {
	    Logger.Log("loadCatalogsCompleted ==> " + obj.Status);
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            loadResourceLocation();
        }
        else
        {
	        Logger.LogError("LoadCatalogsCompleted is failed");
        }
    }

    void loadResourceLocation()
    {
	    Logger.Log("loadResourceLocation");
        var someList = new List<IResourceLocation>();
        AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(someList);

        handle.Completed += locationsLoaded;
    }
    void locationsLoaded(AsyncOperationHandle<IList<IResourceLocation>> obj)
    {
	    Logger.Log("locationsLoaded ==> " + obj.Status);
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            locations = new List<IResourceLocation>(obj.Result);
            loadDependency();
        }
        else
        {
	        Logger.LogError("locationsLoaded is failed");
        }
    }

    void loadDependency()
    {
	    Logger.Log("loadDependency");
        AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync("ReallyDumb");
        handle.Completed += dependencyLoaded;
    }
    void dependencyLoaded(AsyncOperationHandle obj)
    {
	    Logger.Log("dependencyLoaded ==> " + obj.Status);
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            loadAssets();
        }
        else
        {
	        Logger.LogError("dependencyLoaded is Failed");
        }
    }

    private void loadAssets()
    {
        AsyncOperationHandle<IList<GameObject>> handle = Addressables.LoadAssetsAsync<GameObject>(locations, onAssetsCategoryLoaded);
    }
    private void onAssetsCategoryLoaded(GameObject obj)
    {

    }
}