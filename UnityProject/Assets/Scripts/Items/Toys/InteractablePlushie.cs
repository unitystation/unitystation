using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Items.Toys
{
	public class InteractablePlushie : MonoBehaviour, ICheckedInteractable<HandActivate>
	{
		public UnityEvent OnHandActivated = new UnityEvent();

		[SerializeField] private List<string> interactionStrings = new List<string>()
		{
			"hug",
			"pet",
			"squeeze",
			"bite",
		};

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			Chat.AddExamineMsg(interaction.Performer, $"<i>You {interactionStrings.PickRandom()} the {gameObject.ExpensiveName()}</i>");
			OnHandActivated?.Invoke();
		}
	}
}
