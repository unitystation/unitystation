using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class CatalogLoader : MonoBehaviour
{
    public List<IResourceLocation> locations;

    public void loadCatalog(string catalogURL)
    {
        Debug.Log("loadCatalog");
        AsyncOperationHandle<IResourceLocator> handle = Addressables.LoadContentCatalogAsync(catalogURL);
        handle.Completed += loadCatalogsCompleted;
    }
    void loadCatalogsCompleted(AsyncOperationHandle<IResourceLocator> obj)
    {
        Debug.Log("loadCatalogsCompleted ==> " + obj.Status);
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            loadResourceLocation();
        }
        else
        {
            Debug.LogError("LoadCatalogsCompleted is failed");
        }
    }

    void loadResourceLocation()
    {
        Debug.Log("loadResourceLocation");
        var someList = new List<IResourceLocation>();
        AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(someList);
        
        handle.Completed += locationsLoaded;
    }
    void locationsLoaded(AsyncOperationHandle<IList<IResourceLocation>> obj)
    {
        Debug.Log("locationsLoaded ==> " + obj.Status);
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            locations = new List<IResourceLocation>(obj.Result);
            loadDependency();
        }
        else
        {
            Debug.LogError("locationsLoaded is failed");
        }
    }

    void loadDependency()
    {
        Debug.Log("loadDependency");
        AsyncOperationHandle handle = Addressables.DownloadDependenciesAsync("ReallyDumb");
        handle.Completed += dependencyLoaded;
    }
    void dependencyLoaded(AsyncOperationHandle obj)
    {
        Debug.Log("dependencyLoaded ==> " + obj.Status);
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            loadAssets();
        }
        else
        {
            Debug.LogError("dependencyLoaded is Failed");
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