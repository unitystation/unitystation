
using Logs;

/// <summary>
/// Various utilities used for writing populators
/// </summary>
public static class PopulatorUtils
{

	/// <summary>
	/// Tries to get occupation from the context, returns null and logs an error if unable.
	/// </summary>
	/// <param name="context"></param>
	/// <returns></returns>
	public static Occupation TryGetOccupation(PopulationContext context)
	{
		//simply lookup the populator for this occupation
		if (context.SpawnInfo == null)
		{
			Loggy.LogError("PopulationContext does not have any SpawnInfo, ID cannot be auto populated. Please" +
			                " ensure this is only being used for populating a Player during spawn.", Category.EntitySpawn);
			return null;
		}

		if (context.SpawnInfo.SpawnType != SpawnType.Player)
		{
			Loggy.LogErrorFormat("PopulationContext SpawnInfo does not have a SpawnType of Player. Auto ID population" +
			                      " can only be performed when SpawnType is player, otherwise we can't look up their" +
			                      " occupation. SpawnInfo was {0}", Category.EntitySpawn, context.SpawnInfo);
			return null;
		}

		var occupation = context.SpawnInfo.Occupation;
		if (occupation == null)
		{
			Loggy.LogErrorFormat("Unable to get occupation from spawn info, this is likely a bug because" +
			                " it's supposed to be present if SpawnType is Player. SpawnInfo was {0}", Category.EntitySpawn, context.SpawnInfo);
			return null;
		}

		return occupation;
	}
}
