using Objects.Security;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Systems
{
	/// <summary>
	/// Contains a list of all mainstation crewmembers, including traitors and silicons but not, for example, fugitives or wizards.
	/// </summary>
	public class CrewManifestManager : MonoBehaviour
	{
		public static CrewManifestManager Instance;

		/// <summary>
		/// A list of all mainstation crewmembers, including traitors and silicons but not, for example, fugitives or wizards.
		/// </summary>
		public List<CrewManifestEntry> CrewManifest { get; private set; } = new List<CrewManifestEntry>();
		/// <summary>
		/// A list of all security records. By default, includes traitors and silicons but not, for example, fugitives or wizards.
		/// Records can be updated, added or removed during gameplay at the security records console.
		/// </summary>
		public List<SecurityRecord> SecurityRecords { get; private set; } = new List<SecurityRecord>();

		#region Lifecycle

		private void Awake()
		{
			if (Instance == null)
			{
				Instance = this;
			}
			else
			{
				Destroy(this);
			}
		}

		private void OnEnable()
		{
			SceneManager.activeSceneChanged += OnRoundRestart;
		}

		private void OnDisable()
		{
			SceneManager.activeSceneChanged -= OnRoundRestart;
		}

		void OnRoundRestart(Scene scene, Scene newScene)
		{
			CrewManifest.Clear();
			SecurityRecords.Clear();
		}

		#endregion Lifecycle

		/// <summary>
		/// Adds a new employee to the crew manifest, generating new records.
		/// Will not generate certain records for certain jobs, like security records for AI.
		/// </summary>
		/// <returns>A new crew manifest entry.</returns>
		public CrewManifestEntry AddMember(PlayerScript script, JobType jobType)
		{
			CrewManifestEntry entry = new CrewManifestEntry()
			{
				Name = script.playerName,
				JobType = jobType,
			};
			CrewManifest.Add(entry);

			if (jobType == JobType.AI || jobType == JobType.CYBORG) return entry;

			entry.SecurityRecord = GenerateSecurityRecord(script, jobType);
			SecurityRecords.Add(entry.SecurityRecord);

			return entry;
		}

		/// <summary>
		/// Generates a security record and returns it.
		/// Called in RespawnPlayer, so every new respawn creates a record.
		/// </summary>
		public SecurityRecord GenerateSecurityRecord(PlayerScript script, JobType jobType)
		{
			SecurityRecord record = new SecurityRecord();

			record.EntryName = script.playerName;
			record.Age = script.characterSettings.Age.ToString();
			record.Rank = script.mind.occupation.JobType.JobString();
			record.Occupation = OccupationList.Instance.Get(jobType);
			record.Sex = script.characterSettings.Gender.ToString();
			//We don't have races yet. Or I didn't find them.
			record.Species = "Human";
			//I don't know what to put in ID and Fingerprints
			record.ID = $"{UnityEngine.Random.Range(111, 999).ToString()}-{UnityEngine.Random.Range(111, 999).ToString()}";
			record.Fingerprints = UnityEngine.Random.Range(111111, 999999).ToString();
			//Photo stuff
			record.characterSettings = script.characterSettings;

			return record;
		}
	}

	/// <summary>
	/// Contains information on station personnel, including traitors and AI/cyborgs.
	/// Some properties may not be valid - e.g. a security record for AI.
	/// </summary>
	public class CrewManifestEntry
	{
		public string Name { get; set; } = "Crewmember";
		public JobType JobType { get; set; } = JobType.ASSISTANT;

		public SecurityRecord SecurityRecord { get; set; }
		// TODO: add extra records like health here, as needed.
	}
}
