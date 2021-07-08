using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class AddressableBar : MonoBehaviour
{
	public Slider slider;
	public AsyncOperationHandle SetHandle;
	public TMP_Text Text;
	private AddressableCatalogueManager.LoadCounter loadCounter;
	public void Setup(AsyncOperationHandle Handle,  string Name, AddressableCatalogueManager.LoadCounter _loadCounter = null)
	{
		Text.text ="Downloading dependencies for " + Name;
		loadCounter = _loadCounter;
		SetHandle = Handle;
	}
	// Update is called once per frame
    void Update()
    {
	    slider.value = SetHandle.PercentComplete;
	    if (SetHandle.PercentComplete == 1)
	    {
		    if (loadCounter != null)
		    {
			    loadCounter.IncrementAndCheckLoad();
		    }
		    Destroy(this.gameObject);
	    }
    }
}
