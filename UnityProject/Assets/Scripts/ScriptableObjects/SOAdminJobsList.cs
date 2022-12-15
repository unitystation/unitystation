using System.Collections.Generic;
using Antagonists;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

// [CreateAssetMenu(fileName = "adminJobsList", menuName = "ScriptableObjects/AdminJobsList", order = 0)]
namespace ScriptableObjects
{
	public class SOAdminJobsList : SingletonScriptableObject<SOAdminJobsList>
	{
		[Tooltip("List of special jobs admins are allowed to spawn in the game")]
		[FormerlySerializedAs("adminAvailableJobs")]
		[SerializeField]
		[ReorderableList]
		private List<Occupation> specialJobs = new List<Occupation>();
		public List<Occupation> SpecialJobs => specialJobs;

		[Tooltip("List of antagonists admins are allowed to spawn in the game")]
		[SerializeField]
		[ReorderableList]
		private List<Antagonist> antags = new List<Antagonist>();
		public List<Antagonist> Antags => antags;

		public Occupation GetByName(string occupation )
		{
			if (string.IsNullOrEmpty(occupation)) return null;
			foreach (var job in SOAdminJobsList.Instance.SpecialJobs)
			{
				if (job.name != occupation)
				{
					continue;
				}

				return job;
			}

			return null;
		}

	}
}
