using UnityEngine;
using UnityEngine.Serialization;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Items.Traits;
using Util;

namespace US13.Items.Others
{
	public class TransformableItem : MonoBehaviour, ICheckedInteractable<HandApply>
	{
		[Tooltip("Choose an item to spawn.")]
		[SerializeField, FormerlySerializedAs("TraitRequired")]
		private ItemTrait traitRequired = null;

		[Tooltip("Choose an item to spawn.")]
		[SerializeField, FormerlySerializedAs("TransformTo")]
		private GameObject transformTo = null;

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			GameObject ObjectInHand = interaction.HandObject;
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			if (Validations.HasItemTrait(ObjectInHand, traitRequired) == false) return false;

			if (Validations.HasItemTrait(ObjectInHand, CommonTraits.Instance.Welder)
					&& Validations.HasUsedActiveWelder(interaction) == false) return false;

			return true;
		}

		//invoked when the server recieves the interaction request and WIllinteract returns true
		public void ServerPerformInteraction(HandApply interaction)
		{
			ItemAttributesV2 attr = interaction.TargetObject.GetComponent<ItemAttributesV2>();
			if (attr.HasTrait(CommonTraits.Instance.Transforamble))
			{
				Spawn.ServerPrefab(transformTo, interaction.TargetObject.RegisterTile().WorldPositionServer);
				_ = Despawn.ServerSingle(interaction.TargetObject);
			}
		}
	}
}
