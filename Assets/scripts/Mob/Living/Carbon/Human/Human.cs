using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Human : Carbon
{

    // See species.dm for human
    public DamageOverlayType DamageOverlayType = DamageOverlayType.HUMAN; //what kind of damage overlays (if any) appear on our species when wounded?

    // Use this for initialization
	public override void Death(bool gibbed){
		if (CustomNetworkManager.Instance._isServer) {
			if (lastDamager != gameObject.name) {
				PlayerList.Instance.UpdateKillScore(lastDamager);
			}
		}
		mobStat = MobConsciousStat.DEAD;
		base.Death(gibbed);
	}


}
