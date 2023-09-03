using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Items.Implants.Organs;
using Logs;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CharacterCreator
{
	public class BodyPartDropDownReplaceOrgan : BodyPartCustomisationBase
	{
		public Dropdown Dropdown;

		private List<BodyPart> ToChooseFromBodyParts = new List<BodyPart>();

		private BodyPart CurrentBodyPart;

		public BodyPart ParentBodyPart; // can be null with root

		public override void SetUp(CharacterCustomization incharacterCustomization, BodyPart Body_Part, string path)
		{
			base.SetUp(incharacterCustomization, Body_Part, path);
			RelatedBodyPart = Body_Part;
			List<string> itemOptions = new List<string>();
			// Make a list of all available options which can then be passed to the dropdown box
			foreach (var Keyv in incharacterCustomization.ParentDictionary)
			{
				if (Keyv.Value.Contains(Body_Part))
				{
					ParentBodyPart = Keyv.Key;
				}
			}

			CurrentBodyPart = Body_Part;
			ToChooseFromBodyParts = Body_Part.OptionalReplacementOrgan;
			foreach (var Organ in  Body_Part.OptionalReplacementOrgan)
			{
				itemOptions.Add(Organ.name);
			}


			itemOptions.Sort();

			// Ensure "None" is at the top of the option lists
			itemOptions.Insert(0, Body_Part.name);
			Dropdown.AddOptions(itemOptions);
			Dropdown.onValueChanged.AddListener(ItemChange);
		}

		public override void Deserialise(string InData)
		{
			var Newvalue = JsonConvert.DeserializeObject<int>(InData);
			if (Newvalue >= (ToChooseFromBodyParts.Count + 1))
			{
				Newvalue = 0;
			}

			Dropdown.value = Newvalue;
		}

		public override string Serialise()
		{
			return JsonConvert.SerializeObject(Dropdown.value);
		}

		public static void OnPlayerBodyDeserialise(BodyPart bodyPart, string InData)
		{
			var PreviousOptions = JsonConvert.DeserializeObject<int>(InData);
			if (PreviousOptions >= bodyPart.OptionalReplacementOrgan.Count + 1 || PreviousOptions == 0) //0 == Default
			{
				return;
			}

			var ActualIndex = PreviousOptions - 1;

			var spawned = Spawn.ServerPrefab(bodyPart.OptionalReplacementOrgan[ActualIndex].gameObject, spawnManualContents: true);

			var Storage = bodyPart.ContainedIn;

			var IPlayerPossessable = bodyPart.GetComponent<IPlayerPossessable>();




			Storage.OrganStorage.ServerTryAdd(spawned.GameObject);

			if (IPlayerPossessable != null)
			{

				if (IPlayerPossessable.PossessedBy != null)
				{
					IPlayerPossessable.PossessedBy.SetPossessingObject(spawned.GameObject);
				}

				if (IPlayerPossessable.PossessingMind != null)
				{
					IPlayerPossessable.PossessingMind.SetPossessingObject(spawned.GameObject);
				}
			}

			Storage.OrganStorage.ServerTryRemove(bodyPart.gameObject);
			_ = Despawn.ServerSingle(bodyPart.gameObject);

		}

		public void SetDropdownValue(string currentSetting)
		{
			// Find the index of the setting in the dropdown list which matches the currentSetting
			int settingIndex = Dropdown.options.FindIndex(option => option.text == currentSetting);

			if (settingIndex != -1)
			{
				// Make sure FindIndex is successful before changing value
				Dropdown.value = settingIndex;
			}
			else
			{
				Loggy.LogWarning($"Unable to find index of {currentSetting}! Using default", Category.Character);
				Dropdown.value = 0;
			}
		}

		public void DropdownScrollRight()
		{
			// Check if value should wrap around
			if (Dropdown.value < Dropdown.options.Count - 1)
			{
				Dropdown.value++;
			}
			else
			{
				Dropdown.value = 0;
			}
		}

		public void DropdownScrollLeft()
		{
			// Check if value should wrap around
			if (Dropdown.value > 0)
			{
				Dropdown.value--;
			}
			else
			{
				Dropdown.value = Dropdown.options.Count - 1;
			}
		}

		public override void RandomizeInBody(BodyPart Body_Part, LivingHealthMasterBase livingHealth)
		{
			//Doesn't do anything
		}

		public override void RandomizeCharacterCreatorValues()
		{
			Dropdown.value = Random.Range(0, Dropdown.options.Count - 1);
			Refresh();
		}

		public void ItemChange(int newValue)
		{
			Refresh();
		}

		public override void Refresh()
		{
			base.Refresh();

			if (Dropdown.value == 0) //Going to 0
			{
				characterCustomization.RemoveBodyPart(CurrentBodyPart);
				characterCustomization.ParentDictionary[ParentBodyPart].Add(RelatedBodyPart);
				characterCustomization.SetUpBodyPart(RelatedBodyPart, false);
				CurrentBodyPart = RelatedBodyPart;
			}
			else
			{

				if (CurrentBodyPart == RelatedBodyPart) //Don't delete the customisations of this
				{
					characterCustomization.RemoveBodyPart(CurrentBodyPart, false);
				}
				else
				{
					characterCustomization.RemoveBodyPart(CurrentBodyPart);
				}

				var ChosenOption = Dropdown.options[Dropdown.value].text;
				CurrentBodyPart = ToChooseFromBodyParts.First(x => x.name == ChosenOption);
				characterCustomization.ParentDictionary[ParentBodyPart].Add(CurrentBodyPart);
				characterCustomization.SetUpBodyPart(CurrentBodyPart, false);
			}
		}
	}
}