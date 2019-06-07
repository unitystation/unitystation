using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//scrubbers, vents, pumps, etc
public class AdvancedPipe : Pipe
{
	private void Start()
	{
		var registerTile = GetComponent<RegisterTile>();
		if (registerTile.isServer)
		{
			UpdateManager.Instance.Add(UpdateMe);
		}
		base.Start();
	}

	public virtual void UpdateMe()
	{

	}

}
