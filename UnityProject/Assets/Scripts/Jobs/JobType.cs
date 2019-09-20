using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

[Serializable]
public enum JobType
{
	NULL,
	AI,
	ASSISTANT,
	ATMOSTECH,
	BARTENDER,
	BOTANIST,
	CAPTAIN,
	CARGOTECH,
	CHAPLAIN,
	CHEMIST,
	CHIEF_ENGINEER,
	CIVILIAN,
	CLOWN,
	CMO,
	COOK,
	CURATOR,
	CYBORG,
	DETECTIVE,
	DOCTOR,
	ENGSEC,
	ENGINEER,
	GENETICIST,
	HOP,
	HOS,
	JANITOR,
	LAWYER,
	MEDSCI,
	MIME,
	MINER,
	QUARTERMASTER,
	RD,
	ROBOTICIST,
	SCIENTIST,
	SECURITY_OFFICER,
	VIROLOGIST,
	WARDEN,
	SYNDICATE
}

public static class JobTypeExtensions
{
	public static JobType[] DisplayOrder =
	{
		JobType.ASSISTANT, JobType.CAPTAIN, JobType.HOP, JobType.QUARTERMASTER, JobType.CARGOTECH, JobType.MINER,
		JobType.BARTENDER, JobType.COOK, JobType.BOTANIST, JobType.JANITOR, JobType.CLOWN, JobType.MIME,
		JobType.CURATOR, JobType.LAWYER, JobType.CHAPLAIN, JobType.CHIEF_ENGINEER, JobType.ENGINEER, JobType.ATMOSTECH,
		JobType.CMO, JobType.DOCTOR, JobType.CHEMIST, JobType.GENETICIST, JobType.VIROLOGIST, JobType.RD,
		JobType.SCIENTIST, JobType.ROBOTICIST, JobType.HOS, JobType.WARDEN, JobType.DETECTIVE, JobType.SECURITY_OFFICER,
		JobType.AI, JobType.CYBORG
	};
	public static string ToDisplayString(this JobType me)
	{
		var ret = string.Empty;

		switch (me)
		{
			case JobType.NULL:
			case JobType.AI:
			case JobType.ASSISTANT:
			case JobType.BARTENDER:
			case JobType.BOTANIST:
			case JobType.CAPTAIN:
			case JobType.CHAPLAIN:
			case JobType.CHEMIST:
			case JobType.CIVILIAN:
			case JobType.CLOWN:
			case JobType.COOK:
			case JobType.CURATOR:
			case JobType.CYBORG:
			case JobType.DETECTIVE:
			case JobType.ENGINEER:
			case JobType.GENETICIST:
			case JobType.JANITOR:
			case JobType.LAWYER:
			case JobType.MIME:
			case JobType.QUARTERMASTER:
			case JobType.ROBOTICIST:
			case JobType.SCIENTIST:
			case JobType.VIROLOGIST:
			case JobType.WARDEN:
			case JobType.SYNDICATE:
				ret = me.ToString();
				ret = ret.Substring(0, 1) + ret.Substring(1).ToLower();
				break;

			case JobType.ATMOSTECH:
				ret = "Atmospheric Technician";
				break;

			case JobType.CARGOTECH:
				ret = "Cargo Technician";
				break;

			case JobType.CHIEF_ENGINEER:
				ret = "Chief Engineer";
				break;

			case JobType.CMO:
				ret = "Chief Medical Officer";
				break;

			case JobType.DOCTOR:
				ret = "Medical Doctor";
				break;

			case JobType.ENGSEC:
				ret = "Engineering Security";
				break;

			case JobType.HOP:
				ret = "Head of Personnel";
				break;

			case JobType.HOS:
				ret = "Head of Security";
				break;

			case JobType.MEDSCI:
				ret = "Medical Scientist";
				break;

			case JobType.MINER:
				ret = "Shaft Miner";
				break;

			case JobType.RD:
				ret = "Research Director";
				break;

			case JobType.SECURITY_OFFICER:
				ret = "Security Officer";
				break;
		}

		return ret;
	}

	public static Color GetDisplayColor(this JobType me)
	{
		var ret = Color.white;

		switch (me)
		{
			case JobType.ASSISTANT:
			case JobType.CLOWN:
			case JobType.MIME:
			case JobType.CURATOR:
			case JobType.LAWYER:
			case JobType.CHAPLAIN:
				ret = new Color32(221, 221, 221, 255); // #dddddd
				break;

			case JobType.CAPTAIN:
				ret = new Color32(204,204,255, 255); // #ccccff
				break;

			case JobType.HOP:
				ret = new Color32(221,221,255, 255); // #ddddff
				break;

			case JobType.QUARTERMASTER:
				ret = new Color32(215, 176, 136, 255); // #d7b088
				break;

			case JobType.CARGOTECH:
			case JobType.MINER:
				ret = new Color32(220, 186, 151, 255); // #dcba97
				break;

			case JobType.BARTENDER:
			case JobType.COOK:
			case JobType.BOTANIST:
			case JobType.JANITOR:
				ret = new Color32(187,226,145, 255); // #bbe291
				break;

			case JobType.CHIEF_ENGINEER:
				ret = new Color32(255,238,170, 255); // #ffeeaa
				break;

			case JobType.ENGINEER:
			case JobType.ATMOSTECH:
				ret = new Color32(255,245,204, 255); // #fff5cc
				break;

			case JobType.CMO:
				ret = new Color32(255,221,240, 255); // #ffddf0
				break;

			case JobType.DOCTOR:
			case JobType.CHEMIST:
			case JobType.GENETICIST:
			case JobType.VIROLOGIST:
				ret = new Color32(255, 238, 240, 255); // #ffeef0
				break;

			case JobType.RD:
				ret = new Color32(255,221,255, 255); // #ffddff
				break;

			case JobType.SCIENTIST:
			case JobType.ROBOTICIST:
				ret = new Color32(255, 238, 255, 255); // #ffeeff
				break;

			case JobType.HOS:
				ret = new Color32(255,221,221, 255); // #ffdddd
				break;

			case JobType.WARDEN:
			case JobType.DETECTIVE:
			case JobType.SECURITY_OFFICER:
				ret = new Color32(255,238,238, 255); // #ffeeee
				break;

			case JobType.AI:
				ret = new Color32(204,255,204, 255); // #ccffcc
				break;

			case JobType.CYBORG:
				ret = new Color32(221, 255, 221, 255); // #ddffdd
				break;
		}

		return ret;
	}
}