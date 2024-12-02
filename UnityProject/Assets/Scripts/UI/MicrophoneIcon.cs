using System.Collections;
using System.Collections.Generic;
using Shared.Managers;
using UnityEngine;

public class MicrophoneIcon : SingletonManager<MicrophoneIcon>
{

	public override void Start()
	{
		base.Start();
		this.gameObject.SetActive(false);
	}

}
