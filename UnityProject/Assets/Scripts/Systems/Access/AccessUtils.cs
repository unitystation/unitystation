using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Access
{
	public static class AccessUtils
	{
		public static List<AccessRestrictions> GetAllStationAccess()
		{
			return new List<AccessRestrictions>
			{
				AccessRestrictions.SECURITY,
				AccessRestrictions.SEC_DOORS,
				AccessRestrictions.BRIG,
				AccessRestrictions.ARMORY,
				AccessRestrictions.FORENSICS_LOCKERS,
				AccessRestrictions.COURT,
				AccessRestrictions.MEDICAL,
				AccessRestrictions.GENETICS,
				AccessRestrictions.MORGUE,
				AccessRestrictions.RD,
				AccessRestrictions.RND,
				AccessRestrictions.TOXINS,
				AccessRestrictions.CHEMISTRY,
				AccessRestrictions.ENGINE,
				AccessRestrictions.ENGINE_EQUIP,
				AccessRestrictions.MAINT_TUNNELS,
				AccessRestrictions.EXTERNAL_AIRLOCKS,
				AccessRestrictions.CHANGE_IDS,
				AccessRestrictions.AI_UPLOAD,
				AccessRestrictions.TELEPORTER,
				AccessRestrictions.EVA,
				AccessRestrictions.HEADS,
				AccessRestrictions.CAPTAIN,
				AccessRestrictions.ALL_PERSONAL_LOCKERS,
				AccessRestrictions.TECH_STORAGE,
				AccessRestrictions.CHAPEL_OFFICE,
				AccessRestrictions.ATMOSPHERICS,
				AccessRestrictions.KITCHEN,
				AccessRestrictions.BAR,
				AccessRestrictions.JANITOR,
				AccessRestrictions.CREMATORIUM,
				AccessRestrictions.ROBOTICS,
				AccessRestrictions.CARGO,
				AccessRestrictions.CONSTRUCTION,
				AccessRestrictions.AUX_BASE,
				AccessRestrictions.HYDROPONICS,
				AccessRestrictions.LIBRARY,
				AccessRestrictions.LAWYER,
				AccessRestrictions.VIROLOGY,
				AccessRestrictions.CMO,
				AccessRestrictions.QM,
				AccessRestrictions.SURGERY,
				AccessRestrictions.PSYCHOLOGY,
				AccessRestrictions.THEATRE,
				AccessRestrictions.RESEARCH,
				AccessRestrictions.MINING,
				AccessRestrictions.MAILSORTING,
				AccessRestrictions.WEAPONS,
				AccessRestrictions.MECH_MINING,
				AccessRestrictions.MECH_ENGINE,
				AccessRestrictions.MECH_SCIENCE,
				AccessRestrictions.MECH_SECURITY,
				AccessRestrictions.MECH_MEDICAL,
				AccessRestrictions.VAULT,
				AccessRestrictions.MINING_STATION,
				AccessRestrictions.XENOBIOLOGY,
				AccessRestrictions.CE,
				AccessRestrictions.HOP,
				AccessRestrictions.HOS,
				AccessRestrictions.PHARMACY,
				AccessRestrictions.RC_ANNOUNCE,
				AccessRestrictions.KEYCARD_AUTH,
				AccessRestrictions.TCOMSAT,
				AccessRestrictions.GATEWAY,
				AccessRestrictions.MINERAL_STOREROOM,
				AccessRestrictions.MINISAT,
				AccessRestrictions.NETWORK,
				AccessRestrictions.TOXINS_STORAGE
			};
		}

		public static List<AccessRestrictions> GetAllCentcomAccess()
		{
			return new List<AccessRestrictions>
			{
				AccessRestrictions.CENT_GENERAL,
				AccessRestrictions.CENT_THUNDER,
				AccessRestrictions.CENT_SPECOPS,
				AccessRestrictions.CENT_MEDICAL,
				AccessRestrictions.CENT_LIVING,
				AccessRestrictions.CENT_STORAGE,
				AccessRestrictions.CENT_TELEPORTER,
				AccessRestrictions.CENT_CAPTAIN
			};
		}

		public static List<AccessRestrictions> GetERTAccess(JobType job)
		{
			switch (job)
			{
				case JobType.ERT_COMMANDER:
					return GetAllCentcomAccess();
				case JobType.ERT_SECURITY:
					return new List<AccessRestrictions>
					{
						AccessRestrictions.CENT_GENERAL, AccessRestrictions.CENT_SPECOPS,
						AccessRestrictions.CENT_LIVING
					};
				case JobType.ERT_ENGINEER:
					return new List<AccessRestrictions>
					{
						AccessRestrictions.CENT_GENERAL, AccessRestrictions.CENT_SPECOPS,
						AccessRestrictions.CENT_LIVING, AccessRestrictions.CENT_STORAGE
					};
				case JobType.ERT_MEDIC:
					return new List<AccessRestrictions>
					{
						AccessRestrictions.CENT_GENERAL, AccessRestrictions.CENT_SPECOPS,
						AccessRestrictions.CENT_LIVING, AccessRestrictions.CENT_MEDICAL
					};
				case JobType.ERT_CHAPLAIN:
				case JobType.ERT_JANITOR:
				case JobType.ERT_CLOWN:
					return new List<AccessRestrictions>
					{
						AccessRestrictions.CENT_GENERAL, AccessRestrictions.CENT_SPECOPS,
						AccessRestrictions.CENT_LIVING
					};
				default:
					Debug.LogError($"GetERTAccess got unvalid job type as argument. Expected ERT job, got {job.ToString()}." +
					               $" Returning an empty list of access restriction instead.");
					return new List<AccessRestrictions>();
			}
		}

		public static List<AccessRestrictions> GetAllSyndicateAccess()
		{
			return new List<AccessRestrictions>
			{
				AccessRestrictions.SYNDICATE, AccessRestrictions.SYNDICATE_LEADER
			};
		}

		public static string GetRegionName(AccessRegion region)
		{
			switch (region)
			{
				case AccessRegion.All:
					return "All";
				case AccessRegion.General:
					return "General";
				case AccessRegion.Security:
					return "Security";
				case AccessRegion.Medbay:
					return "Medbay";
				case AccessRegion.Research:
					return "Research";
				case AccessRegion.Engineering:
					return "Engineering";
				case AccessRegion.Supply:
					return "Supply";
				case AccessRegion.Command:
					return "Command";
				default:
					return "Unknown";
			}
		}

		public static string GetStationAccessDesc(AccessRestrictions access)
		{
			switch (access)
			{
				case AccessRestrictions.CARGO:
					return "Cargo Bay";
				case AccessRestrictions.SECURITY:
					return "Security";
				case AccessRestrictions.BRIG:
					return "Holding Cells";
				case AccessRestrictions.COURT:
					return "Courtroom";
				case AccessRestrictions.FORENSICS_LOCKERS:
					return "Forensics";
				case AccessRestrictions.MEDICAL:
					return "Medical";
				case AccessRestrictions.GENETICS:
					return "Genetics Lab";
				case AccessRestrictions.MORGUE:
					return "Morgue";
				case AccessRestrictions.RND:
					return "R&D Lab";
				case AccessRestrictions.TOXINS:
					return "Toxins Lab";
				case AccessRestrictions.TOXINS_STORAGE:
					return "Toxins Storage";
				case AccessRestrictions.CHEMISTRY:
					return "Chemistry Lab";
				case AccessRestrictions.RD:
					return "RD Office";
				case AccessRestrictions.BAR:
					return "Bar";
				case AccessRestrictions.JANITOR:
					return "Custodial Closet";
				case AccessRestrictions.ENGINE:
					return "Engineering";
				case AccessRestrictions.ENGINE_EQUIP:
					return "Power and Engineering Equipment";
				case AccessRestrictions.MAINT_TUNNELS:
					return "Maintenance";
				case AccessRestrictions.EXTERNAL_AIRLOCKS:
					return "External Airlocks";
				case AccessRestrictions.CHANGE_IDS:
					return "ID Console";
				case AccessRestrictions.AI_UPLOAD:
					return "AI Chambers";
				case AccessRestrictions.TELEPORTER:
					return "Teleporter";
				case AccessRestrictions.EVA:
					return "EVA";
				case AccessRestrictions.HEADS:
					return "Bridge";
				case AccessRestrictions.CAPTAIN:
					return "Captain";
				case AccessRestrictions.ALL_PERSONAL_LOCKERS:
					return "Personal Lockers";
				case AccessRestrictions.CHAPEL_OFFICE:
					return "Chapel Office";
				case AccessRestrictions.TECH_STORAGE:
					return "Technical Storage";
				case AccessRestrictions.ATMOSPHERICS:
					return "Atmospherics";
				case AccessRestrictions.CREMATORIUM:
					return "Crematorium";
				case AccessRestrictions.ARMORY:
					return "Armory";
				case AccessRestrictions.CONSTRUCTION:
					return "Construction";
				case AccessRestrictions.KITCHEN:
					return "Kitchen";
				case AccessRestrictions.HYDROPONICS:
					return "Hydroponics";
				case AccessRestrictions.LIBRARY:
					return "Library";
				case AccessRestrictions.LAWYER:
					return "Law Office";
				case AccessRestrictions.ROBOTICS:
					return "Robotics";
				case AccessRestrictions.VIROLOGY:
					return "Virology";
				case AccessRestrictions.PSYCHOLOGY:
					return "Psychology";
				case AccessRestrictions.CMO:
					return "CMO Office";
				case AccessRestrictions.QM:
					return "Quartermaster";
				case AccessRestrictions.SURGERY:
					return "Surgery";
				case AccessRestrictions.THEATRE:
					return "Theatre";
				case AccessRestrictions.RESEARCH:
					return "Science";
				case AccessRestrictions.MINING:
					return "Mining";
				case AccessRestrictions.MAILSORTING:
					return "Cargo Office";
				case AccessRestrictions.VAULT:
					return "Main Vault";
				case AccessRestrictions.MINING_STATION:
					return "Mining EVA";
				case AccessRestrictions.XENOBIOLOGY:
					return "Xenobiology Lab";
				case AccessRestrictions.HOP:
					return "HoP Office";
				case AccessRestrictions.HOS:
					return "HoS Office";
				case AccessRestrictions.CE:
					return "CE Office";
				case AccessRestrictions.PHARMACY:
					return "Pharmacy";
				case AccessRestrictions.RC_ANNOUNCE:
					return "RC Announcements";
				case AccessRestrictions.KEYCARD_AUTH:
					return "Keycode Auth.";
				case AccessRestrictions.TCOMSAT:
					return "Telecommunications";
				case AccessRestrictions.GATEWAY:
					return "Gateway";
				case AccessRestrictions.SEC_DOORS:
					return "Brig";
				case AccessRestrictions.MINERAL_STOREROOM:
					return "Mineral Storage";
				case AccessRestrictions.MINISAT:
					return "AI Satellite";
				case AccessRestrictions.WEAPONS:
					return "Weapon Permit";
				case AccessRestrictions.NETWORK:
					return "Network Access";
				case AccessRestrictions.MECH_MINING:
					return "Mining Mech Access";
				case AccessRestrictions.MECH_MEDICAL:
					return "Medical Mech Access";
				case AccessRestrictions.MECH_SECURITY:
					return "Security Mech Access";
				case AccessRestrictions.MECH_SCIENCE:
					return "Science Mech Access";
				case AccessRestrictions.MECH_ENGINE:
					return "Engineering Mech Access";
				case AccessRestrictions.AUX_BASE:
					return "Auxiliary Base";

				default:
					Logger.LogError($"{nameof(GetStationAccessDesc)} got unexpected access definition: {access}. " +
					                $"returning \"Unknown\" instead.");
					return "Unknown";
			}
		}

		public static string GetCentcomAccessDesc(AccessRestrictions access)
		{
			switch (access)
			{
				case AccessRestrictions.CENT_GENERAL:
					return "Code Grey";
				case AccessRestrictions.CENT_THUNDER:
					return "Code Yellow";
				case AccessRestrictions.CENT_STORAGE:
					return "Code Orange";
				case AccessRestrictions.CENT_LIVING:
					return "Code Green";
				case AccessRestrictions.CENT_MEDICAL:
					return "Code White";
				case AccessRestrictions.CENT_TELEPORTER:
					return "Code Blue";
				case AccessRestrictions.CENT_SPECOPS:
					return "Code Black";
				case AccessRestrictions.CENT_CAPTAIN:
					return "Code Gold";
				case AccessRestrictions.CENT_BAR:
					return "Code Scotch";
				default:
					Logger.LogError($"{nameof(GetCentcomAccessDesc)} got unexpected access: {access} " +
					                $"returning Unknown instead.");
					return "Unknown";
			}
		}
	}
}