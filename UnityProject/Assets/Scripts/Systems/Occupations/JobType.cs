﻿using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public enum JobType
{
	//NOTE: To ensure safety of things, like scriptable objects, that are referencing this enum, you must NOT change
	//the ordinals and any new value you add must specify a new ordinal value

	NULL = 0,
	AI = 1,
	ASSISTANT = 2,
	ATMOSTECH = 3,
	BARTENDER = 4,
	BOTANIST = 5,
	CAPTAIN = 6,
	CARGOTECH = 7,
	CHAPLAIN = 8,
	CHEMIST = 9,
	CHIEF_ENGINEER = 10,
	CIVILIAN = 11,
	CLOWN = 12,
	CMO = 13,
	COOK = 14,
	CURATOR = 15,
	CYBORG = 16,
	DETECTIVE = 17,
	DOCTOR = 18,
	ENGSEC = 19,
	ENGINEER = 20,
	GENETICIST = 21,
	HOP = 22,
	HOS = 23,
	JANITOR = 24,
	LAWYER = 25,
	MEDSCI = 26,
	MIME = 27,
	MINER = 28,
	QUARTERMASTER = 29,
	RD = 30,
	ROBOTICIST = 31,
	SCIENTIST = 32,
	SECURITY_OFFICER = 33,
	VIROLOGIST = 34,
	WARDEN = 35,
	SYNDICATE = 36,
	CENTCOMM_OFFICER = 37,
	CENTCOMM_INTERN = 38,
	CENTCOMM_COMMANDER = 39,
	DEATHSQUAD = 40,
	ERT_COMMANDER = 41,
	ERT_SECURITY = 42,
	ERT_MEDIC = 43,
	ERT_ENGINEER = 44,
	ERT_CHAPLAIN = 45,
	ERT_JANITOR = 46,
	ERT_CLOWN = 47,
	TRAITOR = 48,
	CARGONIAN = 49,
	PRISONER = 50,
	FUGITIVE = 51,
	PARAMEDIC = 52,
	PSYCHIATRIST = 53
}

public static class JobCategories
{
	public static readonly List<JobType> CentCommJobs = new List<JobType>()
	{
		JobType.CENTCOMM_OFFICER,
		JobType.CENTCOMM_INTERN,
		JobType.CENTCOMM_COMMANDER,
		JobType.DEATHSQUAD,
		JobType.ERT_COMMANDER,
		JobType.ERT_SECURITY,
		JobType.ERT_MEDIC,
		JobType.ERT_ENGINEER,
		JobType.ERT_CHAPLAIN,
		JobType.ERT_JANITOR,
		JobType.ERT_CLOWN
	};
}
