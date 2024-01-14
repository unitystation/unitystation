using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecureSideHubNotConnectedPopUp : MonoBehaviour
{
	public static SecureSideHubNotConnectedPopUp Instance;

	public IHubNotConnectedPopUp IHubNotConnectedPopUp;

	public void Start()
	{
		Instance = this;
		IHubNotConnectedPopUp = this.GetComponent<IHubNotConnectedPopUp>();
	}

	public void SetUp(string OnFailText, string ClipboardURL)
	{
		IHubNotConnectedPopUp.SetUp(OnFailText, ClipboardURL);
	}
}
