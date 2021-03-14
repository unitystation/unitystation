﻿using System.Collections.Generic;
using System.Text;
using Systems;
using HealthV2;
using Items.PDA;
using Objects.Security;
using UnityEngine;

namespace Player
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
		public InteractableStorage InteractableStorage => interactableStorage;

		/// <summary>
		/// Check if player is wearing a mask
		/// </summary>
		/// <returns>true if player don't wear mask</returns>
		private bool IsFaceVisible => script.ItemStorage.GetNamedItemSlot(NamedSlot.mask).IsEmpty;

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
		private PlayerHealthV2 Health => script.playerHealth;
		private Equipment Equipment => script.Equipment;
		private string VisibleName => script.visibleName;

		private void Awake()
		{
			script = GetComponent<PlayerScript>();
			interactableStorage = GetComponent<InteractableStorage>();
		}

		/// <summary>
		/// Try to find security record by player ID
		/// </summary>
		/// <param name="id">player ID</param>
		/// <param name="securityRecord">player security record if found</param>
		/// <returns>true if security record exists for provided ID</returns>
		private bool TryFindPlayerSecurityRecord(string id, out SecurityRecord securityRecord)
		{
			var records = CrewManifestManager.Instance.SecurityRecords;
			foreach (var record in records)
			{
				if (record.characterSettings.Name.Equals(id) == false)
				{
					continue;
				}

				securityRecord = record;
				return true;
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
				var itemSlot = script.ItemStorage.GetNamedItemSlot(slot);
				if (itemSlot.IsOccupied == false)
				{
					continue;
				}
				// if item is ID card
				if (itemSlot.ItemObject.TryGetComponent(out idCard))
				{
					return true;
				}

				// if item is PDA and IDCard is not null
				if (itemSlot.ItemObject.TryGetComponent<PDALogic>(out var pdaLogic) == false ||
				    pdaLogic.IDCard == null)
				{
					continue;
				}

				idCard = pdaLogic.IDCard;
				return true;
			}

			idCard = null;
			return false;
		}

		public void Examine(GameObject sentByPlayer)
		{
			// if distance is too big or is self-examination, send normal examine message
			if (Vector3.Distance(sentByPlayer.WorldPosServer(), gameObject.WorldPosServer()) >= maxInteractionDistance
			    || sentByPlayer == gameObject)
			{
				Chat.AddExamineMsg(sentByPlayer,
					$"This is <b>{VisibleName}</b>.\n" +
					$"{Equipment.Examine()}" +
					$"<color={LILAC_COLOR}>{Health.GetExamineText()}</color>");
				return;
			}

			// start itemslot observation
			interactableStorage.ItemStorage.ServerAddObserverPlayer(sentByPlayer);
			// send message to enable examination window
			PlayerExaminationMessage.Send(sentByPlayer, this, true);

			//stop observing when target player is too far away
			var relationship = RangeRelationship.Between(
				sentByPlayer,
				gameObject,
				maxInteractionDistance,
				ServerOnObservationEnded
			);
			SpatialRelationship.ServerActivate(relationship);
		}

		private void ServerOnObservationEnded(RangeRelationship cancelled)
		{
			// stop observing item storage
			interactableStorage.ItemStorage.ServerRemoveObserverPlayer(cancelled.obj1.gameObject);
			// send message to disable examination window
			PlayerExaminationMessage.Send(cancelled.obj1.gameObject, this, false);
		}

		public string GetPlayerNameString()
		{
			// first try to get name from id
			// if can't get name from id get visible name
			return TryFindIDCard(out var idCard) ? idCard.RegisteredName : VisibleName;
		}

		public string GetPlayerSpeciesString()
		{
			// if face is visible - get species by face
			if (IsFaceVisible)
				// TODO: get player species
			{
				return "HUMAN";
			}

			//  try get species from security records
			if (TryFindIDCard(out var idCard))
			{
				string id = idCard.RegisteredName;

				if (TryFindPlayerSecurityRecord(id, out var securityRecord))
				{
					return securityRecord.Species;
				}
			}

			return UNKNOWN_VALUE;
		}

		public string GetPlayerJobString()
		{
			// search for ID identity
			if (TryFindIDCard(out var idCard))
			{
				return idCard.JobType.ToString();
			}

			// search for face identity
			if (IsFaceVisible)
			{
				string id = script.characterSettings.Name;
				if (TryFindPlayerSecurityRecord(id, out var securityRecord))
				{
					return securityRecord.Occupation.JobType.ToString();
				}
			}

			return UNKNOWN_VALUE;
		}

		public string GetPlayerStatusString()
		{
			return "";
			//return Health.GetShortStatus();
		}

		/// <summary>
		/// Extra information to be presented on the extended examine view
		/// </summary>
		public string GetAdditionalInformation()
		{

			var result = new StringBuilder();

			if (IsFaceVisible)
			{
				result.Append("Face is visible.\n");
			}

			// result.Append(Health.GetWoundsDescription());

			return result.ToString();
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return string.Empty;
		}
	}
}