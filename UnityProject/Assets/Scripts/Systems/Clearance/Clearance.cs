namespace Systems.Clearance
{
	public enum Clearance
	{
		// Security equipment, security records, gulag item storage, secbots
		Security = 1,
		// Brig cells+timers, permabrig, gulag+gulag shuttle, prisoner management console
		Brig = 2,
		// Armory, gulag teleporter, execution chamber
		Armory = 3,
		//Detective's office, forensics lockers, security+medical records
		ForensicsLockers = 4,
		// Medical general access
		Medical = 5,
		// Morgue access
		Morgue = 6,
		// R&D department and R&D console
		Rnd = 7,
		// Toxins lab and burn chamber
		Toxins = 8,
		// Genetics access
		Genetics = 9,
		// Engineering area, power monitor, power flow control console
		Engine = 10,
		//APCs, EngiVend/YouTool, engineering equipment lockers
		EngineEquip = 11,
		MaintTunnels = 12,
		ExternalAirlocks = 13,
		ChangeIds = 14,
		AIUpload = 15,
		Teleporter = 16,
		Eva = 17,
		// Bridge, EVA storage windoors, gateway shutters, AI integrity restorer, comms console
		Heads = 18,
		Captain = 19,
		AllPersonalLockers = 20,
		ChapelOffice = 21,
		TechStorage = 22,
		Atmospherics = 23,
		Bar = 24,
		Janitor = 25,
		Crematorium = 26,
		Kitchen = 27,
		Robotics = 28,
		Rd = 29,
		Cargo = 30,
		Construction = 31,
		//Allows access to chemistry factory areas on compatible maps
		Chemistry = 32,
		Hydroponics = 33,
		Library = 34,
		Lawyer = 35,
		Virology = 36,
		Cmo = 37,
		Qm = 38,
		Court = 39,
		Surgery = 40,
		Theatre = 41,
		Research = 42,
		Mining = 43,
		Mailsorting = 44,
		Vault = 45,
		MiningStation = 46,
		Xenobiology = 47,
		Ce = 48,
		Hop = 49,
		Hos = 50,
		// Request console announcements
		RcAnnounce = 51,
		// Used for events which require at least two people to confirm them
		KeycardAuth = 52,
		// has access to the entire telecomms satellite / machinery
		Tcomsat = 53,
		Gateway = 54,
		// Outer brig doors, department security posts
		SecDoors = 55,
		// For releasing minerals from the ORM
		MineralStoreroom = 56,
		MiniSat = 57,
		// Weapon authorization for secbots
		Weapons = 58,
		// NTnet diagnostics/monitoring software
		Network = 59,
		// Pharmacy access (Chemistry room in Medbay)
		Pharmacy = 60,
		Psychology = 61,
		// Toxins tank storage room access
		ToxinsStorage = 62,
		// Room and launching.
		AuxBase = 63,

		/*BEGIN CENTCOM ACCESS
		Should leave plenty of room if we need to add more access levels.
		ostly for admin fun times.
		General facilities. CentCom ferry.*/

		CentGeneral = 64,
		// Thunderdome.
		CentThunder = 65,
		// Special Ops. Captain's display case, Marauder and Seraph mechs.
		CentSpecops = 66,

		// Medical/Research
		CentMedical = 67,
		// Living quarters.
		CentLiving = 68,
		// Generic storage areas.
		CentStorage = 69,
		// Teleporter.
		CentTeleporter = 70,
		// Captain's office/ID comp/AI.
		CentCaptain = 71,
		// The non-existent CentCom Bar
		CentBar = 72,

		/*The Syndicate*/

		// General Syndicate Access. Includes Syndicate mechs and ruins.
		Syndicate = 73,
		// Nuke Op Leader Access
		SyndicateLeader = 74,

		/*Away Missions or Ruins
		For generic away-mission/ruin access. Why would normal crew have access to a long-abandoned derelict
		or a 2000 year-old temple?
		Away general facilities. */

		AwayGeneral = 75,
		// Away maintenance
		AwayMaint = 76,
		// Away medical
		AwayMed = 77,
		// Away security
		AwaySec = 78,
		// Away engineering
		AwayEngine = 79,
		//Away generic access
		AwayGeneric1 = 80,
		AwayGeneric2 = 81,
		AwayGeneric3 = 82,
		AwayGeneric4 = 83,

		/*Special, for anything that's basically internal*/

		Bloodcult = 84,

		// Mech Access, allows maintanenace of internal components and altering keycard requirements.
		MechMining = 85,
		MechMedical = 86,
		MechSecurity = 87,
		MechScience = 88,
		MechEngine = 89,

		/*
		 *  New in Unitystation
		 */
		ClownOffice = 90,
		MimeOffice = 91,
	}
}