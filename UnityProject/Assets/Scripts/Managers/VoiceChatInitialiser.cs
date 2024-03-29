using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceChatInitialiser : MonoBehaviour, IClientInteractable<HandActivate>
{
	public bool Interact(HandActivate interaction)
	{
		VoiceChatManager.Instance.SetUp();
		return true;
	}
}
