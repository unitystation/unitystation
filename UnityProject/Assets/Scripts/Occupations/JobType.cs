using System;
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
	DEATHSQUAD = 40
}