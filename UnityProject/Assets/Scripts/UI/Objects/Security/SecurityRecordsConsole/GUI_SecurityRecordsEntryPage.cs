using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Systems;
using Objects.Security;

namespace UI.Objects.Security
{
	public class GUI_SecurityRecordsEntryPage : NetPage
	{
		private SecurityRecord record;
		private GUI_SecurityRecords securityRecordsTab;
		[SerializeField]
		private NetLabel nameText = null;
		[SerializeField]
		private NetLabel idText = null;
		[SerializeField]
		private NetLabel sexText = null;
		[SerializeField]
		private NetLabel ageText = null;
		[SerializeField]
		private NetLabel speciesText = null;
		[SerializeField]
		private NetLabel rankText = null;
		[SerializeField]
		private NetLabel fingerprintText = null;
		[SerializeField]
		private EmptyItemList crimesList = null;
		[SerializeField]
		private NetLabel statusButtonText = null;
		[SerializeField]
		private NetLabel idNameText = null;
		[SerializeField]
		private GameObject popupWindow = null;
		[SerializeField]
		private InputFieldFocus popupWindowEditField = null;
		private NetLabel currentlyEditingField;
		private SecurityRecordCrime currentlyEditingCrime;

		public NetSpriteImage head;
		public NetSpriteImage torso;
		public NetSpriteImage beard;
		public NetSpriteImage hair;
		public NetColorChanger rightLeg;
		public NetColorChanger leftLeg;
		public NetColorChanger rightArm;
		public NetColorChanger leftArm;
		public NetColorChanger eyes;

		public NetSpriteImage jumpsuit;
		public NetSpriteImage exosuit;
		public NetSpriteImage back;
		public NetSpriteImage gloves;
		public NetSpriteImage shoes;
		public NetSpriteImage belt;
		public NetSpriteImage neck;
		public NetSpriteImage underwear;
		public NetSpriteImage socks;

		public void OnOpen(SecurityRecord recordToOpen, GUI_SecurityRecords recordsTab)
		{
			record = recordToOpen;
			securityRecordsTab = recordsTab;
			UpdateEntry();
		}

		private void OnEnable()
		{
			ClosePopup();
		}

		public void RemoveID(ConnectedPlayer player)
		{
			securityRecordsTab.RemoveId(player);
			securityRecordsTab.UpdateIdText(idNameText);
		}

		public void UpdateEntry()
		{
			if (!CustomNetworkManager.Instance._isServer)
			{
				return;
			}

			if (record == null)
			{
				return;
			}

			nameText.SetValueServer(record.EntryName);
			idText.SetValueServer(record.ID);
			sexText.SetValueServer(record.Sex);
			ageText.SetValueServer(record.Age);
			speciesText.SetValueServer(record.Species);
			rankText.SetValueServer(record.Rank);
			fingerprintText.SetValueServer(record.Fingerprints);
			statusButtonText.SetValueServer(record.Status.ToString());

			var characterSettings = record.characterSettings;

			if (characterSettings != null)
			{
				//torso.SetComplicatedValue("human_parts_greyscale", characterSettings.torsoSpriteIndex, characterSettings.skinTone);
				//head.SetComplicatedValue("human_parts_greyscale", characterSettings.headSpriteIndex, characterSettings.skinTone);
				//rightLeg.SetValue = characterSettings.skinTone;
				//leftLeg.SetValue = characterSettings.skinTone;
				//rightArm.SetValue = characterSettings.skinTone;
				//leftArm.SetValue = characterSettings.skinTone;
				//eyes.SetValue = characterSettings.eyeColor;
				//beard.SetComplicatedValue("human_face", characterSettings.facialHairOffset, characterSettings.facialHairColor);
				//hair.SetComplicatedValue("human_face", characterSettings.hairStyleOffset, characterSettings.hairColor);

				//exosuit.SetComplicatedValue("suit", GetSpriteOffset(record.jobOutfit.suit, ItemType.Suit));
				//jumpsuit.SetComplicatedValue("uniform", GetSpriteOffset(record.jobOutfit.uniform, ItemType.Uniform));
				//belt.SetComplicatedValue("belt", GetSpriteOffset(record.jobOutfit.belt, ItemType.Belt));
				//shoes.SetComplicatedValue("feet", GetSpriteOffset(record.jobOutfit.shoes, ItemType.Shoes));
				//back.SetComplicatedValue("back", GetSpriteOffset(record.jobOutfit.backpack, ItemType.Back));
				////neck.SetComplicatedValue("neck", GetSpriteOffset(record.jobOutfit.neck, ItemType.Neck)); //JobOutfits dont have neck slots yet (will need for lawyer)
				//gloves.SetComplicatedValue("hands", GetSpriteOffset(record.jobOutfit.gloves, ItemType.Gloves));
				//underwear.SetComplicatedValue("underwear", characterSettings.underwearOffset);
				//socks.SetComplicatedValue("underwear", characterSettings.socksOffset);
			}

			securityRecordsTab.UpdateIdText(idNameText);
			UpdateCrimesList();
		}

		int GetSpriteOffset(string itemPath, ItemType itemType)
		{
			if (itemPath.Length == 0)
				return -1;
			//var dictionary = ItemAttributes.dm.getObject(itemPath);
			//string item_color = ItemAttributes.TryGetAttr(dictionary, "item_color");
			//string icon_state = ItemAttributes.TryGetAttr(dictionary, "icon_state");
			//string item_state = ItemAttributes.TryGetAttr(dictionary, "item_state");
			//string[] states = { icon_state, item_color, item_state };
			//var offset = ItemAttributes.TryGetClothingOffset(states, itemType);
			return -1;
		}

		public void ChangeStatus()
		{
			switch (record.Status)
			{
				case SecurityStatus.None:
					record.Status = SecurityStatus.Arrest;
					break;
				case SecurityStatus.Arrest:
					record.Status = SecurityStatus.Criminal;
					break;
				case SecurityStatus.Criminal:
					record.Status = SecurityStatus.Parole;
					break;
				case SecurityStatus.Parole:
					record.Status = SecurityStatus.None;
					break;
			}
			statusButtonText.SetValueServer(record.Status.ToString());
		}

		/// <summary>
		/// Opens popup locally. Whole interaction cycle look like this:
		/// 1. Client opens Popup and sets currenty edited field on the server.
		/// 2. Client confirms edit in popup, popup closes locally.
		/// 3. Server sets fields with values from popup.
		/// </summary>
		public void OpenPopup(NetLabel fieldToEdit)
		{
			popupWindow.SetActive(true);
			if (fieldToEdit != null)
			{
				popupWindowEditField.text = fieldToEdit.Value;
			}
		}

		/// <summary>
		/// Set field to edit in popup.
		/// Used for info entry (name, age, etc.)
		/// </summary>
		public void SetEditingField(NetLabel fieldToEdit)
		{
			currentlyEditingField = fieldToEdit;
		}

		/// <summary>
		/// Set Editing field for crime entry.
		/// </summary>
		public void SetEditingField(NetLabel fieldToEdit, SecurityRecordCrime crimeToEdit)
		{
			currentlyEditingField = fieldToEdit;
			currentlyEditingCrime = crimeToEdit;
		}

		/// <summary>
		/// Sets currentlyEditingField value to sent value.
		/// The way it is done is bad, I just couldn't come up with better one.
		/// </summary>
		/// <param name="value">String to set in field.</param>
		public void ConfirmPopup(string value)
		{
			currentlyEditingField.SetValueServer(value);
			string nameBeforeIndex = currentlyEditingField.name.Split('~')[0];
			switch (nameBeforeIndex)
			{
				case "NameText":
					record.EntryName = value;
					break;
				case "IdText":
					record.ID = value;
					break;
				case "SexText":
					record.Sex = value;
					break;
				case "AgeText":
					record.Age = value;
					break;
				case "SpeciesText":
					record.Species = value;
					break;
				case "RankText":
					record.Rank = value;
					break;
				case "FingerprintText":
					record.Fingerprints = value;
					break;
				case "CrimeText":
					currentlyEditingCrime.Crime = value;
					break;
				case "DetailsText":
					currentlyEditingCrime.Details = value;
					break;
				case "AuthorText":
					currentlyEditingCrime.Author = value;
					break;
				case "TimeText":
					currentlyEditingCrime.Time = value;
					break;
			}
		}

		public void ClosePopup()
		{
			popupWindow.SetActive(false);
		}

		public void NewCrime()
		{
			record.Crimes.Add(new SecurityRecordCrime());
			UpdateEntry();
		}

		public void DeleteCrime(SecurityRecordCrime crimeToDelete)
		{
			record.Crimes.Remove(crimeToDelete);
			UpdateEntry();
		}

		public void DeleteRecord()
		{
			CrewManifestManager.Instance.SecurityRecords.Remove(record);
			securityRecordsTab.OpenRecords();
		}

		private void UpdateCrimesList()
		{
			List<SecurityRecordCrime> crimes = record.Crimes;

			crimesList.Clear();
			crimesList.AddItems(crimes.Count);
			for (int i = 0; i < crimes.Count; i++)
			{
				GUI_SecurityRecordsCrime crimeItem = crimesList.Entries[i] as GUI_SecurityRecordsCrime;
				crimeItem.ReInit(crimes[i], this);
			}
		}
	}
}
