using System.Linq;
using Items.PDA;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

namespace Core.Identity
{
	/// <summary>
	/// <seealso cref="SimpleIdentity"/>
	/// <seealso cref="IIdentifiable"/>
	/// More complex version of SimpleIdentity  that is used for characters. This handles characters real names and disguised, artistic names.
	/// </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(PlayerScript))]
	public class CharacterIdentity: NetworkBehaviour, IIdentifiable
	{
		private PlayerScript script;

		[SyncVar(hook = nameof(SetInitialName))]
		private string initialName;

		[SyncVar(hook = nameof(SyncDisplayName))]
		private string visibleName;

		[FormerlySerializedAs("readableIDslots")]
		[Tooltip("Slots from which other players can read ID card data")]
		[SerializeField]
		private NamedSlot[] readableIdSlots =
		{
			NamedSlot.id,
			NamedSlot.belt,
			NamedSlot.leftHand,
			NamedSlot.rightHand
		};

		private void Awake()
		{
			script = GetComponent<PlayerScript>();
		}


		public string DisplayName => visibleName;
		public string InitialName => initialName;

		[Server]
		public void SyncDisplayName(string oldName, string newName)
		{
			throw new System.NotImplementedException();
		}

		[Server]
		public void SetInitialName(string oldName, string newName)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Try to get player ID card
		/// </summary>
		/// <param name="idCard">player id card if found</param>
		/// <returns>true if player has ID card</returns>
		public bool TryFindIDCard(out IDCard idCard)
		{
			var itemSlots = readableIdSlots.SelectMany(slot => script.DynamicItemStorage.GetNamedItemSlots(slot));

			foreach (var itemSlot in itemSlots)
			{
				if (!itemSlot.IsOccupied)
				{
					continue;
				}

				// if item is ID card
				if (itemSlot.ItemObject.TryGetComponent(out idCard))
				{
					return true;
				}

				// if item is PDA and IDCard is not null
				var pdaLogic = itemSlot.ItemObject.GetComponent<PDALogic>();
				var pdaIdCard = pdaLogic.OrNull()?.GetIDCard();

				if (pdaLogic == null || pdaIdCard == null)
				{
					continue;
				}
				idCard = pdaIdCard;
				return true;
			}

			idCard = null;
			return false;
		}
	}
}