using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Systems.Clearance;
using UnityEngine;

public class GroupedAccess : MonoBehaviour, IClearanceSource
{

	private List<IClearanceSource> Sources = new List<IClearanceSource>();


	public IEnumerable<Clearance> IssuedClearance
	{
		get
		{
			return Sources.SelectMany(x => x.IssuedClearance).Distinct();
		}
	}

	public IEnumerable<Clearance> LowPopIssuedClearance
	{
		get
		{
			return Sources.SelectMany(x => x.LowPopIssuedClearance).Distinct();
		}
	}

	public void AddIClearanceSource(IClearanceSource NewIClearanceSource)
	{
		if (NewIClearanceSource.GetType() == typeof(GroupedAccess))
		{
			Loggy.LogError( "prevented adding of GroupedAccess to GroupedAccess,  I don't want no infinite loops, If you need it you can change it So it can catch the loops" );
			return;
		}

		if (Sources.Contains(NewIClearanceSource) == false)
		{
			Sources.Add(NewIClearanceSource);
		}
	}

	public void RemoveIClearanceSource(IClearanceSource NewIClearanceSource)
	{
		if (Sources.Contains(NewIClearanceSource) == false)
		{
			Sources.Remove(NewIClearanceSource);
		}
	}
}