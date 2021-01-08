using System.Collections;
using UnityEngine;

namespace InGameEvents
{
	public class EventMiceMigration : EventMobSpawn
	{
		[Header("Populate the arrays below. E.g. " +
				"\"Due to[bad luck], [a horde of][squeaking things] have[descended] into the[maintenance tunnels].\"")]

		[SerializeField] private string[] reasons;
		[SerializeField] private string[] groupNames;
		[SerializeField] private string[] mobNames;
		[SerializeField] private string[] movementVerbs;
		[SerializeField] private string[] locations;

		protected override string GenerateMessage()
		{
			return $"Due to {reasons.PickRandom()}, {groupNames.PickRandom()} {mobNames.PickRandom()} " +
					$"have {movementVerbs.PickRandom()} into the {locations.PickRandom()}.";
		}
	}
}
