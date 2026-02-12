using System;
using Newtonsoft.Json;
using US13.Systems.Occupations;
using US13.UI.Systems.Lobby;

namespace US13.Systems.Antagonists.Antags.Changeling
{
	public class ChangelingMemories : ICloneable
	{
		public JobType MemoriesJob;
		public string MemoriesName;
		public string MemoriesObjectives;
		public string MemoriesSpecies;
		public Gender MemoriesGender;
		public PlayerPronoun MemoriesPronoun;

		public object Clone()
		{
			string json = JsonConvert.SerializeObject(this);
			return JsonConvert.DeserializeObject<ChangelingMemories>(json);
		}

		public void Form(JobType job, string playerName, string objectives, string species, Gender gender, PlayerPronoun pronoun)
		{
			MemoriesJob = job;
			MemoriesName = playerName;
			MemoriesObjectives = objectives;
			MemoriesSpecies = species;
			MemoriesGender = gender;
			MemoriesPronoun = pronoun;
		}
	}
}