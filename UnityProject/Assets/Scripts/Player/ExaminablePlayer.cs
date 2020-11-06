using System;
using System.Collections;
using System.Collections.Generic;
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

		private void Awake()
		{
			script = GetComponent<PlayerScript>();
			interactableStorage = GetComponent<InteractableStorage>();
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
			string asd = Examine();
			var rootStorage = interactableStorage.ItemStorage.GetRootStorage();

			// if player is not inspecting self
			if (SentByPlayer != gameObject)
			{
				// start itemslot observation
				interactableStorage.ItemStorage.ServerAddObserverPlayer(SentByPlayer);
				// send message to enable examination window
				PlayerExaminationMessage.Send(SentByPlayer, this, true);

				//stop observing when target player is too far away
				var relationship = RangeRelationship.Between(
					SentByPlayer,
					gameObject,
					PlayerScript.interactionDistance,
					ServerOnObservationEndedd
				);
				SpatialRelationship.ServerActivate(relationship);
			}
			// TODO: remove this
			else
				PlayerExaminationMessage.Send(SentByPlayer, this, true);
		}

		private void ServerOnObservationEndedd(RangeRelationship cancelled)
		{
			// stop observing item storage
			interactableStorage.ItemStorage.ServerRemoveObserverPlayer(cancelled.obj1.gameObject);
			// send message to disable examination window
			PlayerExaminationMessage.Send(cancelled.obj1.gameObject, this, false);
		}

		/// <summary>
		/// Try to find security record by player ID
		/// </summary>
		/// <param name="ID">player ID</param>
		/// <param name="securityRecord">player security record if found</param>
		/// <returns>true if security record exists for provided ID</returns>
		private bool TryFindPlayerSecurityRecord(string ID, out SecurityRecord securityRecord)
		{
			List<SecurityRecord> records = SecurityRecordsManager.Instance.SecurityRecords;
			foreach (var record in records)
			{
				// TODO: check ID
				if (record.characterSettings.Name.Equals(ID))
				{
					securityRecord = record;
					return true;
				}
			}

			securityRecord = null;
			return false;
		}

		public string GetPlayerRaceString()
		{
			if (VisibleName.Equals("Unknown"))
				return UNKNOWN_VALUE;

			if (isFaceVisible)
				// TODO: get player race
				return "HUMAN";
			else if (TryFindPlayerSecurityRecord(script.characterSettings.Name, out SecurityRecord securityRecord))
			{
				return securityRecord.Species;
			}

			return UNKNOWN_VALUE;
		}

		public string GetPlayerJobString()
		{
			NamedSlot[] IDslots = new NamedSlot[]
			{
				NamedSlot.id,
				NamedSlot.belt
			};

			// search for ID identity
			foreach (var slot in IDslots)
			{
				ItemSlot itemSlot = script.ItemStorage.GetNamedItemSlot(slot);

				// TODO: check if item is ID and get values
				string ID = script.characterSettings.Name;
				if (itemSlot.IsOccupied && TryFindPlayerSecurityRecord(ID, out SecurityRecord securityRecord))
				{
					return securityRecord.Occupation.JobType.ToString();
				}
			}

			// search for face identity
			if (isFaceVisible)
			{
				// TODO: get ID by face
				string ID = script.characterSettings.Name;
				if (TryFindPlayerSecurityRecord(ID, out SecurityRecord securityRecord))
				{
					return securityRecord.Occupation.JobType.ToString();
				}
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

			// ';' is used to divide sentences to lines
			if (isFaceVisible)
				result += "face is visible;";

			return result;
		}

	}
}
