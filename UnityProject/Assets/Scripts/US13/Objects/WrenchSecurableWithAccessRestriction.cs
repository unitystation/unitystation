using UnityEngine;
using US13.Core.Addressables;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Items;
using US13.Items.Traits;
using US13.Managers;
using US13.Systems.Clearance;
using US13.Systems.Construction;
using Util;

namespace US13.Objects
{
	[RequireComponent(typeof(ClearanceRestricted))]
	[RequireComponent(typeof(ObjectAttributes))]
	[RequireComponent(typeof(WrenchSecurable))]
	public class WrenchSecurableWithAccessRestriction : MonoBehaviour, IFirstInteractable<HandApply>
	{
		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) && IsWrenchInteraction(interaction);
		}

		internal bool IsWrenchInteraction(HandApply interaction)
		{
			if (interaction.TargetObject != gameObject) return false;

			return Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench);
		}

		// Invoked when the server receives the interaction request and WillInteract returns true.
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (HasClearanceToWrench(interaction.Performer))
			{
				// Delegate successful attempts so WrenchSecurable remains authoritative for anchor state and messages.
				GetComponent<WrenchSecurable>().ServerPerformInteraction(interaction);
				return;
			}

			if (IsWrenchInteraction(interaction))
			{
				var objectName = gameObject.ExpensiveName();
				Chat.AddActionMsgToChat(interaction, "You try to wrench down the " + objectName + ", clearance is denied", "");
			}

			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.AccessDenied,
				gameObject.AssumedWorldPosServer(), sourceObj: gameObject);
		}

		internal bool HasClearanceToWrench(GameObject performer)
		{
			return GetComponent<ClearanceRestricted>().HasClearance(performer);
		}
	}
}
