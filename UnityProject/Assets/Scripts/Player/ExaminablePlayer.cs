using System;
using System.Collections;
using System.Collections.Generic;
using Items.PDA;
using Objects.Security;
using Systems;
using UnityEngine;

namespace Assets.Scripts.Player
{
	public class ExaminablePlayer : MonoBehaviour, IExaminable
	{
		private const string LILAC_COLOR = "#b495bf";

		/// <summary>
		/// string that will be returned if value cannot be found while examining other player
		/// </summary>
		private const string UNKNOWN_VALUE = "?";

		private InteractableStorage interactableStorage;

		private PlayerScript script;

		public PlayerHealth Health => script.playerHealth;
		public Equipment Equipment => script.Equipment;
		public InteractableStorage InteractableStorage => interactableStorage;
		public string VisibleName => script.visibleName;

		/// <summary>
		/// Check if player is wearing a mask
		/// </summary>
		/// <returns>true if player don't wear mask</returns>
		private bool isFaceVisible => script.ItemStorage.GetNamedItemSlot(NamedSlot.mask).IsEmpty;

		[Tooltip("Slots from which other players can read ID card data")]
		[SerializeField]
		private NamedSlot[] readableIDslots = new NamedSlot[]
		{
			NamedSlot.id,
			NamedSlot.belt,
			NamedSlot.leftHand,
			NamedSlot.rightHand
		};

		[SerializeField] private float maxInteractionDistance = 3;

		private void Awake()
		{
			script = GetComponent<PlayerScript>();
			interactableStorage = GetComponent<InteractableStorage>();
		}

		/// <summary>
		/// Try to find security record by player ID
		/// </summary>
		/// <param name="ID">player ID</param>
		/// <param name="securityRecord">player security record if found</param>
		/// <returns>true if security record exists for provided ID</returns>
		private bool TryFindPlayerSecurityRecord(string ID, out SecurityRecord securityRecord)
		{
			List<SecurityRecord> records = CrewManifestManager.Instance.SecurityRecords;
			foreach (var record in records)
			{
				if (record.characterSettings.Name.Equals(ID))
				{
					securityRecord = record;
					return true;
				}
			}

			securityRecord = null;
			return false;
		}

		/// <summary>
		/// Try to get player IDcard
		/// </summary>
		/// <param name="idCard">player id card if found</param>
		/// <returns>true if player has id card</returns>
		private bool TryFindIDCard(out IDCard idCard)
		{
			foreach (var slot in readableIDslots)
			{
				ItemSlot itemSlot = script.ItemStorage.GetNamedItemSlot(slot);
				if (itemSlot.IsOccupied)
				{
					// if item is ID card
					if (itemSlot.ItemObject.TryGetComponent<IDCard>(out idCard))
						return true;

					// if item is PDA and IDCard is not null
					if (itemSlot.ItemObject.TryGetComponent<PDALogic>(out PDALogic pdaLogic) && pdaLogic.IDCard != null)
					{
						idCard = pdaLogic.IDCard;
						return true;
					}
				}
			}

			idCard = null;
			return false;
		}

		/// <summary>
		/// This is just a simple initial implementation of IExaminable to health;
		/// can potentially be extended to return more details and let the server
		/// figure out what to pass to the client, based on many parameters such as
		/// role, medical skill (if they get implemented), equipped medical scanners,
		/// etc. In principle takes care of building the string from start to finish,
		/// so logic generating examine text can be completely separate from examine
		/// request or netmessage processing.
		/// </summary>
		public string Examine(Vector3 worldPos = default)
		{
			return $"This is <b>{VisibleName}</b>.\n" +
					$"{Equipment.Examine()}" +
					$"<color={LILAC_COLOR}>{Health.GetExamineText()}</color>";
		}

		public void Examine(GameObject SentByPlayer)
		{
			// if player is not inspecting self and distance is not too big
			if (SentByPlayer != gameObject && Vector3.Distance(SentByPlayer.WorldPosServer(), gameObject.WorldPosServer()) <= maxInteractionDistance)
			{
				// start itemslot observation
				interactableStorage.ItemStorage.ServerAddObserverPlayer(SentByPlayer);
				// send message to enable examination window
				PlayerExaminationMessage.Send(SentByPlayer, this, true);

				//stop observing when target player is too far away
				var relationship = RangeRelationship.Between(
					SentByPlayer,
					gameObject,
					maxInteractionDistance,
					ServerOnObservationEndedd
				);
				SpatialRelationship.ServerActivate(relationship);
			}
		}

		private void ServerOnObservationEndedd(RangeRelationship cancelled)
		{
			// stop observing item storage
			interactableStorage.ItemStorage.ServerRemoveObserverPlayer(cancelled.obj1.gameObject);
			// send message to disable examination window
			PlayerExaminationMessage.Send(cancelled.obj1.gameObject, this, false);
		}

		public string GetPlayerNameString()
		{
			// first try to get name from id
			if (TryFindIDCard(out IDCard idCard))
				return idCard.RegisteredName;

			// if can't get name from id get visible name
			return VisibleName;
		}

		public string GetPlayerSpeciesString()
		{
			// if face is visible - get species by face
			if (isFaceVisible)
				// TODO: get player species
				return "HUMAN";
			// else - try get species from security records
			else if (TryFindIDCard(out IDCard idCard))
			{
				string ID = idCard.RegisteredName;
				if (TryFindPlayerSecurityRecord(ID, out SecurityRecord securityRecord))
					return securityRecord.Species;
			}

			return UNKNOWN_VALUE;
		}

		public string GetPlayerJobString()
		{
			// search for ID identity
			if (TryFindIDCard(out IDCard idCard))
				return idCard.JobType.ToString();

			// search for face identity
			if (isFaceVisible)
			{
				string ID = script.characterSettings.Name;
				if (TryFindPlayerSecurityRecord(ID, out SecurityRecord securityRecord))
					return securityRecord.Occupation.JobType.ToString();
			}

			return UNKNOWN_VALUE;
		}

		public string GetPlayerStatusString()
		{
			// TODO: GetPlayerStatusString
			return "TODO";
		}

		/// <summary>
		/// Get list of informations divided by ';'
		/// </summary>
		public string GetAdditionalInformations()
		{
			string result = "";

			// '\n' is used to divide sentence to lines
			if (isFaceVisible)
				result += "face is visible\n";

			return result;
		}

	}
}