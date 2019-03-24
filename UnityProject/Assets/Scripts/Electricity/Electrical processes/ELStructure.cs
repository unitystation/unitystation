using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ELStructure
{
	public static void CircuitSearchLoop(IElectricityIO Thiswire, IProvidePower ProvidingPower)
	{

	}
	public static void DirectionOutput(GameObject SourceInstance, IElectricityIO Thiswire,CableLine RelatedLine = null)
	{
		int SourceInstanceID = SourceInstance.GetInstanceID();
		if (!(Thiswire.Data.Upstream.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.Upstream[SourceInstanceID] = new HashSet<IElectricityIO>();
		}
		if (!(Thiswire.Data.Downstream.ContainsKey(SourceInstanceID)))
		{
			Thiswire.Data.Downstream[SourceInstanceID] = new HashSet<IElectricityIO>();
		}
		if (Thiswire.Data.connections.Count <= 0)
		{
			Thiswire.FindPossibleConnections();
		}
		for (int i = 0; i < Thiswire.Data.connections.Count; i++)
		{
			if (!(Thiswire.Data.Upstream[SourceInstanceID].Contains(Thiswire.Data.connections[i])) && (!(Thiswire == Thiswire.Data.connections[i])))
			{
				bool pass = true;
				if (RelatedLine != null)
				{
					//Logger.Log ("wowowowwo ");
					if (RelatedLine.Covering.Contains(Thiswire.Data.connections[i]))
					{
						pass = false;
						//Logger.Log ("Failed" + Thiswire.Data.connections [i].GameObject ().name);
					}
				}
				if (!(Thiswire.Data.Downstream[SourceInstanceID].Contains(Thiswire.Data.connections[i])) && pass)
				{
					Thiswire.Data.Downstream[SourceInstanceID].Add(Thiswire.Data.connections[i]);

					Thiswire.Data.connections[i].DirectionInput(SourceInstance, Thiswire);
				}
			}
		}
	}
}

