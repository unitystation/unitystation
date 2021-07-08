using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AddressableDownloadPopUp : MonoBehaviour
{
	public AddressableBar DownloadBar;

	public void DownloadDependencies(List<IResourceLocation> Content, AddressableCatalogueManager.LoadCounter loadCounter = null)
	{
		foreach (var Assets in Content)
		{
			var newbar = Instantiate(DownloadBar, gameObject.transform);
			var Handle = Addressables.DownloadDependenciesAsync(Assets.InternalId);
			newbar.Setup(Handle, Assets.InternalId,loadCounter);
		}

	}

}
