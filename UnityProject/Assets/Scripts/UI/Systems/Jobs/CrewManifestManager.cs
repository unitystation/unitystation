using Objects.Security;
using System.Collections.Generic;
using Messages.Server;
using Mirror;
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
		/// <para>ServerSide Only</para>
		/// </summary>
		public List<CrewManifestEntry> CrewManifest { get; private set; } = new List<CrewManifestEntry>();
		/// <summary>
		/// A list of all security records. By default, includes traitors and silicons but not, for example, fugitives or wizards.
		/// <para>Records can be updated, added or removed during gameplay at the security records console.</para>
		/// <para>ServerSide Only</para>
		/// </summary>
		public List<SecurityRecord> SecurityRecords { get; private set; } = new List<SecurityRecord>();

		/// <summary>
		/// A list of crew member jobs and how many there are
		/// <para>Server and Client Side valid</para>
		/// </summary>
		public Dictionary<JobType, int> Jobs { get; private set; } = new Dictionary<JobType, int>();

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

		#region Crew Manifest

		/// <summary>
		/// Adds a new employee to the crew manifest, generating new records.
		/// Will not generate certain records for certain jobs, like security records for AI.
		/// </summary>
		/// <returns>A new crew manifest entry.</returns>
		public CrewManifestEntry AddMember(PlayerScript script, JobType jobType)
		{
			CrewManifestEntry entry = new CrewManifestEntry()
			{
				Name = script.characterSettings.Name,
				JobType = jobType,
			};
			CrewManifest.Add(entry);

			//Updates clients about the amount of this job
			ServerAddJob(jobType);

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

			record.EntryName = script.characterSettings.Name;
			record.Age = script.characterSettings.Age.ToString();
			record.Rank = jobType.JobString();
			record.Occupation = OccupationList.Instance.Get(jobType);
			record.Sex = script.characterSettings.BodyType.ToString();
			record.Species = script.characterSettings.Species.ToString();
			//I don't know what to put in ID
			record.ID = $"{UnityEngine.Random.Range(111, 999).ToString()}-{UnityEngine.Random.Range(111, 999).ToString()}";

			var Data = "";
			foreach (var Hand in script.DynamicItemStorage.GetHandSlots())
			{
				Data += Hand.ItemStorage.gameObject.GetInstanceID() + " , ";
			}
			record.Fingerprints = Data;
			//Photo stuff
			record.characterSettings = script.characterSettings;

			return record;
		}

		#endregion

		#region PlayerJobs

		/// <summary>
		/// Changes a specific job in the job list
		/// </summary>
		public void ChangeJobList(JobType job, int newAmount)
		{
			if (Jobs.ContainsKey(job))
			{
				Jobs[job] = newAmount;
				return;
			}

			//Dont need to make new entry for 0 or less
			if(newAmount <= 0) return;

			Jobs.Add(job, newAmount);
		}

		/// <summary>
		/// Sets the data to the job list
		/// </summary>
		public void SetJobList(Dictionary<JobType, int> newData)
		{
			Jobs = newData;
		}

		[Server]
		private void ServerAddJob(JobType job)
		{
			if (Jobs.ContainsKey(job))
			{
				//Add one
				Jobs[job] += 1;
			}
			else
			{
				//Set to one
				Jobs.Add(job, 1);
			}

			//Update players
			UpdateJobCountsMessage.Send(job, Jobs[job]);
		}

		/// <summary>
		/// Server clears the list and updates clients
		/// </summary>
		[Server]
		public void ServerClearList()
		{
			//Tell clients to clear
			SetJobCountsMessage.SendClearMessage();
		}

		/// <summary>
		/// Client clears the list and updates clients
		/// </summary>
		[Client]
		public void ClientClearList()
		{
			Jobs.Clear();
		}

		/// <summary>
		/// Gets how many of a specific job there is, only crew jobs
		/// </summary>
		public int GetJobAmount(JobType job)
		{
			if (Jobs.TryGetValue(job, out var value))
			{
				return value;
			}

			return 0;
		}

		#endregion
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
