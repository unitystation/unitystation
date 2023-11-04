using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AllClientsObservableMind : NetworkBehaviour //so Stuff like what body your inhabiting and is ghosted/Non-important mind can be synchronised
{
	[SyncVar] public uint IDActivelyControlling;

	[SyncVar] public bool IsGhosting;

	[SyncVar] public bool NonImportantMind = false;

	public GameObject ControllingObject
	{
		get
		{
			if (IsGhosting) return this.gameObject;
			if (IDActivelyControlling is NetId.Invalid or NetId.Empty) return this.gameObject;
			var Possessable = CustomNetworkManager.Spawned[IDActivelyControlling].GetComponent<IPlayerPossessable>();
			return 	Possessable.ControllingObject;
		}
	}
}
