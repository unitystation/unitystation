using System.Collections;
using System.Collections.Generic;
using Shared.Systems.ObjectConnection;
using Systems.Scenes;
using UnityEngine;

public class ExclusionZoneMono : MonoBehaviour, IMultitoolSlaveable
{
	public MultitoolConnectionType ConType => MultitoolConnectionType.MaintGeneratorExclusionZone;

	public bool CanRelink => true;

	[SerializeField] private MaintGenerator maintGenerator;

	public IMultitoolMasterable Master
	{
		get => maintGenerator;
		set
		{
			maintGenerator = (MaintGenerator) value;
		}

	}
	public bool RequireLink => true;

	public bool TrySetMaster(GameObject performer, IMultitoolMasterable master)
	{
		if (maintGenerator != null)
		{
			if (maintGenerator.exclusionZonesMono.Contains(this))
			{
				maintGenerator.exclusionZonesMono.Remove(this);
			}
		}

		Master = master;
		if (maintGenerator != null)
		{
			if (maintGenerator.exclusionZonesMono.Contains(this) == false)
			{
				maintGenerator.exclusionZonesMono.Add(this);
			}
		}
		return true;
	}

	public void SetMasterEditor(IMultitoolMasterable master)
	{
		if (Master != null)
		{
			var MaintGenerator = (Master as MaintGenerator);
			if (MaintGenerator.exclusionZonesMono.Contains(this))
			{
				MaintGenerator.exclusionZonesMono.Remove(this);
			}
		}

		Master = master;
		if (Master != null)
		{
			var MaintGenerator = (Master as MaintGenerator);
			if (MaintGenerator.exclusionZonesMono.Contains(this) == false)
			{
				MaintGenerator.exclusionZonesMono.Add(this);
			}
		}
	}

	public Vector2Int Offset;
	public Vector2Int Size;
}