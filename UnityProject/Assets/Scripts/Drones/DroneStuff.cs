using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Drones
{
	/// <summary>
	/// Pretty much a carbon copy of SpawnedAntag with some stuff moved around.
	/// As such, it does pretty much the same things.
	/// </summary>
	public class DroneStuff
	{
		[Range (1, 3)]
		public static int i;
		/// <summary>
		/// The drone the player is now.
		/// </summary>
		public readonly Drone Drone;
		/// <summary>
		/// Who's controlling this drone?
		/// </summary>
		public readonly Mind DroneMind;
		/// <summary>
		/// The current drone ruleset.
		/// </summary>
		public IEnumerable<DroneRules> Rules;
		private DroneStuff(Drone drone, Mind droneMind, IEnumerable<DroneRules> rules)
		{
			Drone = drone;
			DroneMind = droneMind;
			Rules = rules;
		}
		public static DroneStuff Create(Drone drone, Mind droneMind, IEnumerable<DroneRules> rules)
		{
			return new DroneStuff(drone, droneMind, rules);
		}
		public string GetRulesForPlayer()
		{
			var ruleList = Rules.ToList();
			StringBuilder objSB = new StringBuilder($"</i><size=26>You are a <b>{ruleList[i].DroneName}</b>!</size>\n", 200);
			objSB.AppendLine($"Your current ruleset is {ruleList[1].RulesetName}.\nYour rules are:");
			objSB.AppendLine($"1. {ruleList[i].Rule1}");
			objSB.AppendLine($"2. {ruleList[i].Rule2}");
			objSB.AppendLine($"3. {ruleList[i].Rule3}");
			objSB.AppendLine("<i>");
			return objSB.ToString();
		}
	}
}