using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class Human : Carbon
{

    // See species.dm for human
    public DamageOverlayType DamageOverlayType = DamageOverlayType.HUMAN; //what kind of damage overlays (if any) appear on our species when wounded?

    // Use this for initialization
	public override void Death(bool gibbed){
		
		if (CustomNetworkManager.Instance._isServer) {
			if (lastDamager != gameObject.name) {
				PlayerNetworkActions pNet = GetComponent<PlayerNetworkActions>();
				pNet.RpcSpawnGhost();
			    
				PlayerMove pM = GetComponent<PlayerMove>();
				pM.isGhost = true;
				pM.allowInput = true;
				PlayerList.Instance.UpdateKillScore(lastDamager);
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdSendAllertMessage("<color=red><b>"+lastDamager+"</b> has killed <b>"+gameObject.name+"</b></color>", true); //killfeed
			}
		}
		mobStat = MobConsciousStat.DEAD;
		base.Death(gibbed);
	}


}
