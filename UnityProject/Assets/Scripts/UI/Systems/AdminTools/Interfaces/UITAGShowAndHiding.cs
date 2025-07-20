using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UITAGShowAndHiding : MonoBehaviour
{

	public TAGType TAGType;

	public List<TAGType> NeedsAny;

	public List<TAGType> NeedsAll;

	public bool RequireBoth = false;

	public void Awake()
	{
		PlayerList.OnPermissionsChange += CheckState;
	}

	public void CheckState()
	{
		if (NeedsAny.Count > 0 || NeedsAll.Count > 0)
		{
			bool Active = true;
			if (NeedsAny.Count > 0)
			{
				Active = NeedsAny.Any(x => PlayerList.HasTAGClient(x.Value));
				if (RequireBoth && Active == false)
				{
					this.gameObject.SetActive(Active);
					return;
				}
			}

			if (NeedsAll.Count > 0)
			{
				Active = NeedsAny.All(x => PlayerList.HasTAGClient(x.Value));
			}

			this.gameObject.SetActive(Active);
		}
		else
		{
			this.gameObject.SetActive(PlayerList.HasTAGClient(TAGType.Value));
		}


	}

	public void OnEnable()
	{
		CheckState();

	}

	public void OnDestroy()
	{
		PlayerList.OnPermissionsChange -= CheckState;
	}
}
