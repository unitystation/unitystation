using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AccessType
{
	/// <summary>
	/// Used to set access restrictions on ID cards and doors
	/// </summary>
	public enum Access
	{
		security = 1,
		brig = 2,
		armory = 3,
		forensics_lockers = 4,
		medical = 5,
		morgue = 6,
		rnd = 7,        // Research and Development
		tox_storage = 8,    // Toxins mixing and storage
		genetics = 9,
		engine = 10,    // Power Engines
		engine_equip = 11,  // Engineering Foyer
		maint_tunnels = 12,
		external_airlocks = 13,
		emergency_storage = 14,
		change_ids = 15,
		ai_upload = 16,
		teleporter = 17,
		eva = 18,
		heads = 19,
		captain = 20,
		all_personal_lockers = 21,
		chapel_office = 22,
		tech_storage = 23,
		atmospherics = 24,
		bar = 25,
		janitor = 26,
		crematorium = 27,
		kitchen = 28,
		robotics = 29,
		rd = 30,
		cargo = 31, // Cargo Bay
		construction = 32,  // Vacant office, etc
		chemistry = 33,
		cargo_bot = 34,
		hydroponics = 35,
		manufacturing = 36,
		library = 37,
		lawyer = 38,
		virology = 39,
		cmo = 40,
		qm = 41,
		court = 42,
		clown = 43,
		mime = 44,
		surgery = 45,
		theatre = 46,
		science = 47,       // Research Division hallway
		mining = 48,
		mining_office = 49, //not in use
		mailsorting = 50,   // Cargo Office
		mint = 51,
		mint_vault = 52,
		heads_vault = 53,
		mining_station = 54,
		xenobiology = 55,
		ce = 56,
		hop = 57,
		hos = 58,
		RC_announce = 59, //Request console announcements
		keycard_auth = 60,//Used for events which require at least two people to confirm them
		tcomsat = 61, // has access to the entire telecomms satellite / machinery
		gateway = 62,
		sec_doors = 63, // Security front doors
		psychiatrist = 64, // Psychiatrist's office
		salvage_captain = 65, // Salvage ship captain's quarters
		weapons = 66,//Weapon authorization for secbots
		taxi = 67, // Taxi drivers
		shop = 68,
		//BEGIN CENTCOM ACCESS	        
		cent_general = 101,//General facilities.
		cent_thunder = 102,//Thunderdome.
		cent_specops = 103,//Death Commando.
		cent_medical = 104,//Medical/Research
		cent_living = 105,//Living quarters.
		cent_storage = 106,//Generic storage areas.
		cent_teleporter = 107,//Teleporter.
		cent_creed = 108,//Creed's office/ID comp
		cent_captain = 109,//Captain's office/ID comp/AI.
		cent_ert = 110,//ERT.

		//The Syndicate
		syndicate = 150,//General Syndicate Access     

		//MONEY
		crate_cash = 200,
	}

	/// <summary>
	/// To identify the card type
	/// </summary>
	public enum IDCardType
	{
		standard = 1,
		command = 2,
		captain = 3
	}
}
