using System;
using System.Collections.Generic;
using Logs;
using Mirror;
using UnityEngine;

namespace Player
{
	public class PlayerStats : NetworkBehaviour {

		private Dictionary<Stat, Dictionary<string, float>> stats;
		
		public enum Stat
		{
			MeleeDamage,
			LocalChatRange,
		}
		
		private void Awake()
		{
			stats = new();
			foreach (Stat stat in Enum.GetValues(typeof(Stat)))
			{
				stats.Add(stat, new());
			}
		}
				
		public float GetTotalStat(Stat stat)
		{
			var val = 0f;
			foreach (var value in stats[stat])
			{
				val += value.Value;
			}
			return val;
		}
		
		public void AddModifier(Stat stat, string source, float value)
		{
			stats[stat].Add(source, value);
		}
		
		public void RemoveModifier(Stat stat, string source)
		{
			stats[stat].Remove(source);
		}
	}
}
