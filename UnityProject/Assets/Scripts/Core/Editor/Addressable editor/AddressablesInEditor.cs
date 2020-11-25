using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;


public class AddressablesInEditor : EditorWindow
{
	public static List<IResourceLocation> locations = new List<IResourceLocation>();

	// [MenuItem("Tools/LoadCatalog")]
	// public static void LoadCatalog()
	// {
		// Addressables.InitializeAsync();
		// var cool =
			// "Q:/Fast programmes/Unity station_unity station/unitystation/UnityProject/AddressablePackingProjects/SoundAndMusic/ServerData/StandaloneWindows64/catalog_2020.11.13.00.24.18.json";
		// Debug.Log("loadCatalog");
		// Addressables.LoadContentCatalogAsync(cool);
	// }


	public static void loadCatalogsCompleted()
	{
		var cool = Addressables.InstantiateAsync("test", Vector3.zero, Quaternion.identity);
	}

	private static async void OnCompleted(AsyncOperationHandle<IResourceLocator> obj)
	{
		//Addressables.InstantiateAsync("test");
	}

	public static void InitializeAsync()
	{

	}

}