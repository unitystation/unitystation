using System.Collections.Generic;
using System.Linq;
using HealthV2;
using Newtonsoft.Json;

namespace UI.CharacterCreator
{
	public class BodyPartDropDownOrgans : BodyPartCustomisationBase
	{
		public MultiDropDownSelector Dropdown;

		public List<BodyPart> ToChooseFromBodyParts = new List<BodyPart>();

		public IBodyPartDropDownOrgans thisRelatedBodyPart;

		public BodyPart CurrentBodyPart;

		public List<bool> PreviousOptions = new List<bool>();

		public override void Deserialise(string InData)
		{
			PreviousOptions = JsonConvert.DeserializeObject<List<bool>>(InData);
			Dropdown.SetValues(PreviousOptions);
		}

		public override string Serialise()
		{
			return JsonConvert.SerializeObject(Dropdown.value);
		}

		public static void PlayerBodyDeserialise(BodyPart Body_Part, string InData, LivingHealthMasterBase LivingHealthMasterBase)
		{
			var PreviousOptions = JsonConvert.DeserializeObject<List<bool>>(InData);
			for (int i = 0; i < PreviousOptions.Count; i++)
			{
				if (PreviousOptions[i])
				{
					var spawned = Spawn.ServerPrefab(Body_Part.OptionalOrgans[i].gameObject, spawnManualContents: true);
					LivingHealthMasterBase.BodyPartStorage.ServerTryAdd(spawned.GameObject);
				}
			}
		}

		public override void OnPlayerBodyDeserialise(BodyPart Body_Part, string InData, LivingHealthMasterBase LivingHealthMasterBase)
		{
			PlayerBodyDeserialise(Body_Part, InData, LivingHealthMasterBase);
		}

		public void SetUp(CharacterCustomization incharacterCustomization, IBodyPartDropDownOrgans Body_Part, string path)
		{
			//base.SetUp(incharacterCustomization, Body_Part, path);
			characterCustomization = incharacterCustomization;
			//Text.text = Body_Part.name;
			thisRelatedBodyPart = Body_Part;
			List<string> itemOptions = null;
			// Make a list of all available options which can then be passed to the dropdown box

			ToChooseFromBodyParts = thisRelatedBodyPart.OptionalOrgans;
			itemOptions = thisRelatedBodyPart.OptionalOrgans.Select(gameObject => gameObject.name).ToList();


			itemOptions.Sort();
			foreach (var I in itemOptions)
			{
				PreviousOptions.Add(false);
			}

			Dropdown.AddOptions(itemOptions);
			Dropdown.onValueChanged += ItemChange;
		}

		public override void SetUp(CharacterCustomization incharacterCustomization, BodyPart Body_Part, string path)
		{
			base.SetUp(incharacterCustomization, Body_Part, path);
			thisRelatedBodyPart = (IBodyPartDropDownOrgans)Body_Part;
			List<string> itemOptions = null;
			// Make a list of all available options which can then be passed to the dropdown box

			ToChooseFromBodyParts = RelatedBodyPart.OptionalOrgans;
			itemOptions = RelatedBodyPart.OptionalOrgans.Select(gameObject => gameObject.name).ToList();


			itemOptions.Sort();

			Dropdown.AddOptions(itemOptions);
			Dropdown.onValueChanged += ItemChange;
		}

		public void SetDropdownValue(string currentSetting)
		{
			// Find the index of the setting in the dropdown list which matches the currentSetting
			// int settingIndex = Dropdown.options.FindIndex(option => option.text == currentSetting);

			// if (settingIndex != -1)
			// {
			// Make sure FindIndex is successful before changing value
			// Dropdown.value = settingIndex;
			// }
			// else
			// {
			// Logger.LogWarning($"Unable to find index of {currentSetting}! Using default", Category.Character);
			// Dropdown.value = 0;
			// }
		}

		public void ItemChange(List<bool> newValue)
		{
			Refresh();
		}

		public override void Refresh()
		{
			base.Refresh();

			for (int i = 0; i < Dropdown.value.Count; i++)
			{
				if (Dropdown.value[i] && PreviousOptions[i])
				{
					continue;
				}

				if (Dropdown.value[i] && PreviousOptions[i] == false)
				{
					var ChosenOption = Dropdown.options[i];
					var Selected = ToChooseFromBodyParts.First(x => x.name == ChosenOption);
					if (RelatedBodyPart != null)
					{
						characterCustomization.ParentDictionary[RelatedBodyPart].Add(Selected);
					}
					characterCustomization.SetUpBodyPart(Selected);
					continue;
				}

				if (Dropdown.value[i] == false && PreviousOptions[i])
				{
					var ChosenOption = Dropdown.options[i];
					var Selected = ToChooseFromBodyParts.First(x => x.name == ChosenOption);
					characterCustomization.RemoveBodyPart(Selected);
					continue;
				}


			}
			PreviousOptions = new List<bool>(Dropdown.value);
		}
	}
}

namespace HealthV2
{
	public interface IBodyPartDropDownOrgans
	{
		List<BodyPart> OptionalOrgans {
			get;
		}
	}
}
