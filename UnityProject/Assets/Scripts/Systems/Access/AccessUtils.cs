using System;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Access
{
	public static class AccessUtils
	{
		public static List<AccessDefinitions> GetAllStationAccess()
		{
			return new List<AccessDefinitions>
			{
				AccessDefinitions.SECURITY,
				AccessDefinitions.SEC_DOORS,
				AccessDefinitions.BRIG,
				AccessDefinitions.ARMORY,
				AccessDefinitions.FORENSICS_LOCKERS,
				AccessDefinitions.COURT,
				AccessDefinitions.MEDICAL,
				AccessDefinitions.GENETICS,
				AccessDefinitions.MORGUE,
				AccessDefinitions.RD,
				AccessDefinitions.RND,
				AccessDefinitions.TOXINS,
				AccessDefinitions.CHEMISTRY,
				AccessDefinitions.ENGINE,
				AccessDefinitions.ENGINE_EQUIP,
				AccessDefinitions.MAINT_TUNNELS,
				AccessDefinitions.EXTERNAL_AIRLOCKS,
				AccessDefinitions.CHANGE_IDS,
				AccessDefinitions.AI_UPLOAD,
				AccessDefinitions.TELEPORTER,
				AccessDefinitions.EVA,
				AccessDefinitions.HEADS,
				AccessDefinitions.CAPTAIN,
				AccessDefinitions.ALL_PERSONAL_LOCKERS,
				AccessDefinitions.TECH_STORAGE,
				AccessDefinitions.CHAPEL_OFFICE,
				AccessDefinitions.ATMOSPHERICS,
				AccessDefinitions.KITCHEN,
				AccessDefinitions.BAR,
				AccessDefinitions.JANITOR,
				AccessDefinitions.CREMATORIUM,
				AccessDefinitions.ROBOTICS,
				AccessDefinitions.CARGO,
				AccessDefinitions.CONSTRUCTION,
				AccessDefinitions.AUX_BASE,
				AccessDefinitions.HYDROPONICS,
				AccessDefinitions.LIBRARY,
				AccessDefinitions.LAWYER,
				AccessDefinitions.VIROLOGY,
				AccessDefinitions.CMO,
				AccessDefinitions.QM,
				AccessDefinitions.SURGERY,
				AccessDefinitions.PSYCHOLOGY,
				AccessDefinitions.THEATRE,
				AccessDefinitions.RESEARCH,
				AccessDefinitions.MINING,
				AccessDefinitions.MAILSORTING,
				AccessDefinitions.WEAPONS,
				AccessDefinitions.MECH_MINING,
				AccessDefinitions.MECH_ENGINE,
				AccessDefinitions.MECH_SCIENCE,
				AccessDefinitions.MECH_SECURITY,
				AccessDefinitions.MECH_MEDICAL,
				AccessDefinitions.VAULT,
				AccessDefinitions.MINING_STATION,
				AccessDefinitions.XENOBIOLOGY,
				AccessDefinitions.CE,
				AccessDefinitions.HOP,
				AccessDefinitions.HOS,
				AccessDefinitions.PHARMACY,
				AccessDefinitions.RC_ANNOUNCE,
				AccessDefinitions.KEYCARD_AUTH,
				AccessDefinitions.TCOMSAT,
				AccessDefinitions.GATEWAY,
				AccessDefinitions.MINERAL_STOREROOM,
				AccessDefinitions.MINISAT,
				AccessDefinitions.NETWORK,
				AccessDefinitions.TOXINS_STORAGE
			};
		}

		public static List<AccessDefinitions> GetAllCentcomAccess()
		{
			return new List<AccessDefinitions>
			{
				AccessDefinitions.CENT_GENERAL,
				AccessDefinitions.CENT_THUNDER,
				AccessDefinitions.CENT_SPECOPS,
				AccessDefinitions.CENT_MEDICAL,
				AccessDefinitions.CENT_LIVING,
				AccessDefinitions.CENT_STORAGE,
				AccessDefinitions.CENT_TELEPORTER,
				AccessDefinitions.CENT_CAPTAIN
			};
		}

		public static List<AccessDefinitions> GetERTAccess(JobType job)
		{
			switch (job)
			{
				case JobType.ERT_COMMANDER:
					return GetAllCentcomAccess();
				case JobType.ERT_SECURITY:
					return new List<AccessDefinitions>
					{
						AccessDefinitions.CENT_GENERAL, AccessDefinitions.CENT_SPECOPS,
						AccessDefinitions.CENT_LIVING
					};
				case JobType.ERT_ENGINEER:
					return new List<AccessDefinitions>
					{
						AccessDefinitions.CENT_GENERAL, AccessDefinitions.CENT_SPECOPS,
						AccessDefinitions.CENT_LIVING, AccessDefinitions.CENT_STORAGE
					};
				case JobType.ERT_MEDIC:
					return new List<AccessDefinitions>
					{
						AccessDefinitions.CENT_GENERAL, AccessDefinitions.CENT_SPECOPS,
						AccessDefinitions.CENT_LIVING, AccessDefinitions.CENT_MEDICAL
					};
				case JobType.ERT_CHAPLAIN:
				case JobType.ERT_JANITOR:
				case JobType.ERT_CLOWN:
					return new List<AccessDefinitions>
					{
						AccessDefinitions.CENT_GENERAL, AccessDefinitions.CENT_SPECOPS,
						AccessDefinitions.CENT_LIVING
					};
				default:
					Debug.LogError($"GetERTAccess got unvalid job type as argument. Expected ERT job, got {job.ToString()}." +
					               $" Returning an empty list of access restriction instead.");
					return new List<AccessDefinitions>();
			}
		}

		public static List<AccessDefinitions> GetAllSyndicateAccess()
		{
			return new List<AccessDefinitions>
			{
				AccessDefinitions.SYNDICATE, AccessDefinitions.SYNDICATE_LEADER
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

		public static string GetStationAccessDesc(AccessDefinitions access)
		{
			switch (access)
			{
				case AccessDefinitions.CARGO:
					return "Cargo Bay";
				case AccessDefinitions.SECURITY:
					return "Security";
				case AccessDefinitions.BRIG:
					return "Holding Cells";
				case AccessDefinitions.COURT:
					return "Courtroom";
				case AccessDefinitions.FORENSICS_LOCKERS:
					return "Forensics";
				case AccessDefinitions.MEDICAL:
					return "Medical";
				case AccessDefinitions.GENETICS:
					return "Genetics Lab";
				case AccessDefinitions.MORGUE:
					return "Morgue";
				case AccessDefinitions.RND:
					return "R&D Lab";
				case AccessDefinitions.TOXINS:
					return "Toxins Lab";
				case AccessDefinitions.TOXINS_STORAGE:
					return "Toxins Storage";
				case AccessDefinitions.CHEMISTRY:
					return "Chemistry Lab";
				case AccessDefinitions.RD:
					return "RD Office";
				case AccessDefinitions.BAR:
					return "Bar";
				case AccessDefinitions.JANITOR:
					return "Custodial Closet";
				case AccessDefinitions.ENGINE:
					return "Engineering";
				case AccessDefinitions.ENGINE_EQUIP:
					return "Power and Engineering Equipment";
				case AccessDefinitions.MAINT_TUNNELS:
					return "Maintenance";
				case AccessDefinitions.EXTERNAL_AIRLOCKS:
					return "External Airlocks";
				case AccessDefinitions.CHANGE_IDS:
					return "ID Console";
				case AccessDefinitions.AI_UPLOAD:
					return "AI Chambers";
				case AccessDefinitions.TELEPORTER:
					return "Teleporter";
				case AccessDefinitions.EVA:
					return "EVA";
				case AccessDefinitions.HEADS:
					return "Bridge";
				case AccessDefinitions.CAPTAIN:
					return "Captain";
				case AccessDefinitions.ALL_PERSONAL_LOCKERS:
					return "Personal Lockers";
				case AccessDefinitions.CHAPEL_OFFICE:
					return "Chapel Office";
				case AccessDefinitions.TECH_STORAGE:
					return "Technical Storage";
				case AccessDefinitions.ATMOSPHERICS:
					return "Atmospherics";
				case AccessDefinitions.CREMATORIUM:
					return "Crematorium";
				case AccessDefinitions.ARMORY:
					return "Armory";
				case AccessDefinitions.CONSTRUCTION:
					return "Construction";
				case AccessDefinitions.KITCHEN:
					return "Kitchen";
				case AccessDefinitions.HYDROPONICS:
					return "Hydroponics";
				case AccessDefinitions.LIBRARY:
					return "Library";
				case AccessDefinitions.LAWYER:
					return "Law Office";
				case AccessDefinitions.ROBOTICS:
					return "Robotics";
				case AccessDefinitions.VIROLOGY:
					return "Virology";
				case AccessDefinitions.PSYCHOLOGY:
					return "Psychology";
				case AccessDefinitions.CMO:
					return "CMO Office";
				case AccessDefinitions.QM:
					return "Quartermaster";
				case AccessDefinitions.SURGERY:
					return "Surgery";
				case AccessDefinitions.THEATRE:
					return "Theatre";
				case AccessDefinitions.RESEARCH:
					return "Science";
				case AccessDefinitions.MINING:
					return "Mining";
				case AccessDefinitions.MAILSORTING:
					return "Cargo Office";
				case AccessDefinitions.VAULT:
					return "Main Vault";
				case AccessDefinitions.MINING_STATION:
					return "Mining EVA";
				case AccessDefinitions.XENOBIOLOGY:
					return "Xenobiology Lab";
				case AccessDefinitions.HOP:
					return "HoP Office";
				case AccessDefinitions.HOS:
					return "HoS Office";
				case AccessDefinitions.CE:
					return "CE Office";
				case AccessDefinitions.PHARMACY:
					return "Pharmacy";
				case AccessDefinitions.RC_ANNOUNCE:
					return "RC Announcements";
				case AccessDefinitions.KEYCARD_AUTH:
					return "Keycode Auth.";
				case AccessDefinitions.TCOMSAT:
					return "Telecommunications";
				case AccessDefinitions.GATEWAY:
					return "Gateway";
				case AccessDefinitions.SEC_DOORS:
					return "Brig";
				case AccessDefinitions.MINERAL_STOREROOM:
					return "Mineral Storage";
				case AccessDefinitions.MINISAT:
					return "AI Satellite";
				case AccessDefinitions.WEAPONS:
					return "Weapon Permit";
				case AccessDefinitions.NETWORK:
					return "Network Access";
				case AccessDefinitions.MECH_MINING:
					return "Mining Mech Access";
				case AccessDefinitions.MECH_MEDICAL:
					return "Medical Mech Access";
				case AccessDefinitions.MECH_SECURITY:
					return "Security Mech Access";
				case AccessDefinitions.MECH_SCIENCE:
					return "Science Mech Access";
				case AccessDefinitions.MECH_ENGINE:
					return "Engineering Mech Access";
				case AccessDefinitions.AUX_BASE:
					return "Auxiliary Base";

				default:
					Logger.LogError($"{nameof(GetStationAccessDesc)} got unexpected access definition: {access}. " +
					                $"returning \"Unknown\" instead.");
					return "Unknown";
			}
		}

		public static string GetCentcomAccessDesc(AccessDefinitions access)
		{
			switch (access)
			{
				case AccessDefinitions.CENT_GENERAL:
					return "Code Grey";
				case AccessDefinitions.CENT_THUNDER:
					return "Code Yellow";
				case AccessDefinitions.CENT_STORAGE:
					return "Code Orange";
				case AccessDefinitions.CENT_LIVING:
					return "Code Green";
				case AccessDefinitions.CENT_MEDICAL:
					return "Code White";
				case AccessDefinitions.CENT_TELEPORTER:
					return "Code Blue";
				case AccessDefinitions.CENT_SPECOPS:
					return "Code Black";
				case AccessDefinitions.CENT_CAPTAIN:
					return "Code Gold";
				case AccessDefinitions.CENT_BAR:
					return "Code Scotch";
				default:
					Logger.LogError($"{nameof(GetCentcomAccessDesc)} got unexpected access: {access} " +
					                $"returning Unknown instead.");
					return "Unknown";
			}
		}
	}
}