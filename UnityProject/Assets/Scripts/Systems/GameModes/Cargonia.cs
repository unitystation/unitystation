using System;
using System.Collections.Generic;
using Logs;
using UnityEngine;
using Player;

namespace GameModes
{
	[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Cargonia")]
	public class Cargonia : GameMode
	{
		private List<JobType> rebelJob;

		public override void SetupRound()
		{
			base.SetupRound();

			//Select a random department
			var rnd = new System.Random();
			var rebelDep = (Departments) rnd.Next(Enum.GetNames(typeof(Departments)).Length);
			rebelJob = rebelJobs[rebelDep];
			GameManager.Instance.Rebels = rebelJob;
			Loggy.LogFormat("The using {0} as the rebel department!", Category.GameMode, rebelDep);

		}

		// TODO switch this for the Department ScriptableObjects
		private  enum Departments
		{
			Engineering,
			Science,
			Medical,
			Service,
			Supply
		}

		private readonly Dictionary<Departments, List<JobType>> rebelJobs = new Dictionary<Departments, List<JobType>>() {
			{Departments.Engineering,
				new List<JobType>{JobType.CHIEF_ENGINEER, JobType.ENGINEER, JobType.ATMOSTECH}},
			{Departments.Science,
				new List<JobType>{JobType.RD, JobType.GENETICIST, JobType.SCIENTIST, JobType.ROBOTICIST}},
			{Departments.Medical,
				new List<JobType>{JobType.CMO, JobType.DOCTOR, JobType.CHEMIST, JobType.VIROLOGIST}},
			{Departments.Service,
				new List<JobType>{JobType.CLOWN, JobType.JANITOR, JobType.BARTENDER, JobType.COOK, JobType.BOTANIST, JobType.MIME, JobType.CHAPLAIN, JobType.CURATOR}},
			{Departments.Supply,
				new List<JobType>{JobType.QUARTERMASTER, JobType.CARGOTECH, JobType.MINER}}
		};

		protected override bool ShouldSpawnAntag(PlayerSpawnRequest spawnRequest)
		{
			return rebelJob.Contains(spawnRequest.RequestedOccupation.JobType);
		}
	}
}
