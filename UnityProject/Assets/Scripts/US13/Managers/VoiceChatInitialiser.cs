using UnityEngine;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;

namespace US13.Managers
{
	public class VoiceChatInitialiser : MonoBehaviour, IClientInteractable<HandActivate>
	{
		public bool Interact(HandActivate interaction)
		{
			VoiceChatManager.Instance.SetUp();
			return true;
		}
	}
}
