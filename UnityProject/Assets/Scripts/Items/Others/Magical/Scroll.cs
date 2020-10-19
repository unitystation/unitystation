using UnityEngine;
using Mirror;

namespace Items.Scrolls
{
	public abstract class Scroll : NetworkBehaviour, IExaminable, ICheckedInteractable<HandActivate>
	{
		[Tooltip("How many uses this scroll can give. -1 if infinite.")]
		[SerializeField]
		protected int initialChargesCount = 4;

		public int ChargesRemaining { get; protected set; }
		public bool HasCharges => ChargesRemaining != 0;

		protected virtual void Awake()
		{
			ChargesRemaining = initialChargesCount;
		}

		public virtual string Examine(Vector3 worldPos = default)
		{
			if (initialChargesCount == -1)
			{
				return "It can be used infinitely.";
			} 
			else
			{
				return $"It has {ChargesRemaining} {(ChargesRemaining == 1 ? "charge" : "charges")} remaining.";
			}
		}

		public virtual bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public virtual void ServerPerformInteraction(HandActivate interaction)
		{
			if (HasChargesRemaining(interaction.Performer))
			{
				ActivateScroll(interaction);
				ChargesRemaining--;
			}
		}

		/// <summary>
		/// Determines whether there are charges remaining.
		/// If none, and if a recipient is supplied, an examine message explains that there are no more charges.
		/// </summary>
		/// <param name="messageRecipient">The recipient to send a message to, if supplied.</param>
		/// <returns>True if the scroll has charges remaining or is infinitely charged.</returns>
		protected bool HasChargesRemaining(GameObject messageRecipient = null)
		{
			if (HasCharges) return true;

			if (messageRecipient != null)
			{
				Chat.AddExamineMsgFromServer(messageRecipient, "The scroll has no more charges!");
			}
			return false;
		}

		/// <summary>
		/// Occurs when the scroll is activated in hand (clicked).
		/// </summary>
		/// <param name="interaction">The interaction by which the scroll was activated.</param>
		protected virtual void ActivateScroll(HandActivate interaction) { }
	}
}
