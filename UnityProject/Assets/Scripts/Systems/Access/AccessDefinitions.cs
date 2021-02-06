namespace Systems.Access
{
	public enum AccessDefinitions
	{
		NONE = 0,
		// Security equipment, security records, gulag item storage, secbots
		SECURITY = 1,
		// Brig cells+timers, permabrig, gulag+gulag shuttle, prisoner management console
		BRIG = 2,
		// Armory, gulag teleporter, execution chamber
		ARMORY = 3,
		//Detective's office, forensics lockers, security+medical records
		FORENSICS_LOCKERS = 4,
		// Medical general access
		MEDICAL = 5,
		// Morgue access
		MORGUE = 6,
		// R&D department and R&D console
		RND = 7,
		// Toxins lab and burn chamber
		TOXINS = 8,
		// Genetics access
		GENETICS = 9,
		// Engineering area, power monitor, power flow control console
		ENGINE = 10,
		//APCs, EngiVend/YouTool, engineering equipment lockers
		ENGINE_EQUIP = 11,
		MAINT_TUNNELS = 12,
		EXTERNAL_AIRLOCKS = 13,
		CHANGE_IDS = 14,
		AI_UPLOAD = 15,
		TELEPORTER = 16,
		EVA = 17,
		// Bridge, EVA storage windoors, gateway shutters, AI integrity restorer, comms console
		HEADS = 18,
		CAPTAIN = 19,
		ALL_PERSONAL_LOCKERS = 20,
		CHAPEL_OFFICE = 21,
		TECH_STORAGE = 22,
		ATMOSPHERICS = 23,
		BAR = 24,
		JANITOR = 25,
		CREMATORIUM = 26,
		KITCHEN = 27,
		ROBOTICS = 28,
		RD = 29,
		CARGO = 30,
		CONSTRUCTION = 31,
		//Allows access to chemistry factory areas on compatible maps
		CHEMISTRY = 32,
		HYDROPONICS = 33,
		LIBRARY = 34,
		LAWYER = 35,
		VIROLOGY = 36,
		CMO = 37,
		QM = 38,
		COURT = 39,
		SURGERY = 40,
		THEATRE = 41,
		RESEARCH = 42,
		MINING = 43,
		MAILSORTING = 44,
		VAULT = 45,
		MINING_STATION = 46,
		XENOBIOLOGY = 47,
		CE = 48,
		HOP = 49,
		HOS = 50,
		// Request console announcements
		RC_ANNOUNCE = 51,
		// Used for events which require at least two people to confirm them
		KEYCARD_AUTH = 52,
		// has access to the entire telecomms satellite / machinery
		TCOMSAT = 53,
		GATEWAY = 54,
		// Outer brig doors, department security posts
		SEC_DOORS = 55,
		// For releasing minerals from the ORM
		MINERAL_STOREROOM = 56,
		MINISAT = 57,
		// Weapon authorization for secbots
		WEAPONS = 58,
		// NTnet diagnostics/monitoring software
		NETWORK = 59,
		// Pharmacy access (Chemistry room in Medbay)
		PHARMACY = 60,
		PSYCHOLOGY = 61,
		// Toxins tank storage room access
		TOXINS_STORAGE = 62,
		// Room and launching.
		AUX_BASE = 63,

		/*BEGIN CENTCOM ACCESS
		Should leave plenty of room if we need to add more access levels.
		ostly for admin fun times.
		General facilities. CentCom ferry.*/

		CENT_GENERAL = 64,
		// Thunderdome.
		CENT_THUNDER = 65,
		// Special Ops. Captain's display case, Marauder and Seraph mechs.
		CENT_SPECOPS = 66,

		// Medical/Research
		CENT_MEDICAL = 67,
		// Living quarters.
		CENT_LIVING = 68,
		// Generic storage areas.
		CENT_STORAGE = 69,
		// Teleporter.
		CENT_TELEPORTER = 70,
		// Captain's office/ID comp/AI.
		CENT_CAPTAIN = 71,
		// The non-existent CentCom Bar
		CENT_BAR = 72,

		/*The Syndicate*/

		// General Syndicate Access. Includes Syndicate mechs and ruins.
		SYNDICATE = 73,
		// Nuke Op Leader Access
		SYNDICATE_LEADER = 74,

		/*Away Missions or Ruins
		For generic away-mission/ruin access. Why would normal crew have access to a long-abandoned derelict
		or a 2000 year-old temple?
		Away general facilities. */

		AWAY_GENERAL = 75,
		// Away maintenance
		AWAY_MAINT = 76,
		// Away medical
		AWAY_MED = 77,
		// Away security
		AWAY_SEC = 78,
		// Away engineering
		AWAY_ENGINE = 79,
		//Away generic access
		AWAY_GENERIC1 = 80,
		AWAY_GENERIC2 = 81,
		AWAY_GENERIC3 = 82,
		AWAY_GENERIC4 = 83,

		/*Special, for anything that's basically internal*/

		BLOODCULT = 84,

		// Mech Access, allows maintanenace of internal components and altering keycard requirements.
		MECH_MINING = 85,
		MECH_MEDICAL = 86,
		MECH_SECURITY = 87,
		MECH_SCIENCE = 88,
		MECH_ENGINE = 89,
	}
}
