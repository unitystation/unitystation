using System.Collections;
using System.Collections.Generic;
using Initialisation;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class EditorCatalogueManager : MonoBehaviour, IInitialise
{
	public InitialisationSystems Subsystem => InitialisationSystems.Addressables;
	void IInitialise.Initialise()
	{
		Addressables.InitializeAsync();

		var cool =
			Application.dataPath + "/AddressablePackingProjects/SoundAndMusic/ServerData/StandaloneWindows64/catalog_2020.11.13.00.24.18.json";
		Debug.Log("loadCatalog   >  " + cool);
		Addressables.LoadContentCatalogAsync(cool);
	}

}
