using System.Collections;
using System.Collections.Generic;
using Shared.Managers;
using UnityEngine;

public class MicrophoneIcon : SingletonManager<MicrophoneIcon>
{

	public void Start()
	{
		base.Start();
		this.gameObject.SetActive(false);
	}

}
