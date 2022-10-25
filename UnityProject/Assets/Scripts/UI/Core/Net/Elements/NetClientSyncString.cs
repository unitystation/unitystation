using System.Collections;
using System.Collections.Generic;
using UI.Core.NetUI;
using UnityEngine;

public class NetClientSyncString : NetServerSyncString
{
	public override ElementMode InteractionMode => ElementMode.Normal;
}
