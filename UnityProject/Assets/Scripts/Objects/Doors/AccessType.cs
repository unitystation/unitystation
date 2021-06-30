
	/// <summary>
	///     Used to set access restrictions on ID cards and doors
	/// </summary>
	public enum Access
	{
		ai_upload = 1,

		//all_personal_lockers = 2,
		armory = 3,
		atmospherics = 4,
		bar = 5,
		brig = 6,
		captain = 7,
		cargo = 8, // Cargo Bay
		cargo_bot = 9,
		ce = 10,

		//cent_captain = 11,
		//cent_creed = 12,
		//cent_ert = 13,
		//cent_general = 14,
		//cent_living = 15,
		//cent_medical = 16,
		//cent_specops = 17,
		//cent_storage = 18,
		//cent_teleporter = 19,
		//cent_thunder = 20,
		change_ids = 21,
		chapel_office = 22,
		chemistry = 23,
		clown = 24,
		cmo = 25,
		construction = 26, // Vacant office, etc
		court = 27,

		//crate_cash = 28,
		crematorium = 29,
		emergency_storage = 30,
		engine_room = 31, // Access to engine room
		engineering = 32, // Engineering Foyer
		eva = 33,
		external_airlocks = 34,
		forensics_lockers = 35,
		gateway = 36,
		genetics = 37,
		heads = 38,
		heads_vault = 39,
		hop = 40,
		hos = 41,
		hydroponics = 42,
		janitor = 43,
		keycard_auth = 44, //Used for events which require at least two people to confirm them
		kitchen = 45,
		lawyer = 46,
		library = 47,
		mailsorting = 48, // Cargo Office
		maint_tunnels = 49,
		manufacturing = 50,
		medical = 51,
		mime = 52,
		mining = 53,
		mining_office = 54,
		mining_station = 55,

		//mint = 56,
		//mint_vault = 57,
		morgue = 58,
		psychiatrist = 59,
		qm = 60,
		RC_announce = 61, //Request console announcements
		rd = 62,
		rnd_lab = 63, // Research and Development lab
		robotics = 64,

		//salvage_captain = 65,
		science = 66, // Research Division hallway
		sec_doors = 67, // Security front doors
		security = 68,
		shop = 69,
		surgery = 70,
		syndicate = 71,

		//taxi = 72,
		tcomsat = 73, // has access to the entire telecomms satellite / machinery
		tech_storage = 74,
		teleporter = 75,
		theatre = 76,
		tox_storage = 77, // Toxins mixing and storage
		virology = 78,
		weapons = 79, //Weapon authorization for secbots
		xenobiology = 80,
		centcom = 81,
	}

	/// <summary>
	///     To identify the card type
	/// </summary>
	public enum IDCardType
	{
		standard = 1,
		command = 2,
		captain = 3
	}
