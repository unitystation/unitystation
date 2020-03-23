using UnityEngine;
using Antagonists;
using System.Collections;
using System.Collections.Generic;
using System;

[CreateAssetMenu(menuName="ScriptableObjects/GameModes/Cargonia")]
public class Cargonia : GameMode
{
	/// <summary>
	/// Set up the station for the game mode
	/// </summary>
	private List<JobType> RebelJob;

	public override void SetupRound()
	{
		Logger.Log("Setting up traitor round!", Category.GameMode);
		//Select a random department
		var rnd = new System.Random();
		var RebelDep = (Departments) rnd.Next(Enum.GetNames(typeof(Departments)).Length);
		RebelJob = RebelJobs[RebelDep];
		GameManager.Instance.Rebels = RebelJob;
	}
	/// <summary>
	/// Begin the round
	/// </summary>
	public override void StartRound()
	{
		Logger.Log("Starting traitor round!", Category.GameMode);
		base.StartRound();
	}
	// /// <summary>
	// /// Check if the round should end yet
	// /// </summary>
	// public override void CheckEndCondition()
	// {
	// 	Logger.Log("Check end round conditions!", Category.GameMode);
	// }

	// /// <summary>
	// /// End the round and display any relevant reports
	// /// </summary>
	// public override void EndRound()
	// {

	// }
	private  enum Departments 
	{
		Engineering,
		Science,
		Medical,
		Service,
		Supply
	}

	private readonly Dictionary<Departments, List<JobType>> RebelJobs = new Dictionary<Departments, List<JobType>>() {
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
		return RebelJob.Contains(spawnRequest.RequestedOccupation.JobType);
	}
}