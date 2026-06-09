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
	public class WrenchSecurableWithAccessRestriction : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		private ClearanceRestricted clearanceRestrictions;

		private ObjectAttributes objectAttributes;

		void Awake()
		{
			objectAttributes = GetComponent<ObjectAttributes>();
			clearanceRestrictions = GetComponent<ClearanceRestricted>();
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			//start with the default HandApply WillInteract logic.
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			return !(clearanceRestrictions.HasClearance(interaction.Performer) && Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench));
		}

		//invoked when the server recieves the interaction request and WIllinteract returns true
		public void ServerPerformInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.HandObject, CommonTraits.Instance.Wrench))
			{
				string objectName = objectAttributes.name;
				//Notify player that they are unable to wrench down barrier
				Chat.AddActionMsgToChat(interaction, "You try to wrench down the " + objectName + ", access is denied", "");
			}
			else {
				Chat.AddActionMsgToChat(interaction, "ACCESS DENIED","");
			}
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.AccessDenied,
				gameObject.AssumedWorldPosServer(), sourceObj: gameObject);
		}
	}
}