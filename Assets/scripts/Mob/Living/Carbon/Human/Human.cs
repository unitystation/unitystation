using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;

public class Human : Carbon
{

    // See species.dm for human
    public DamageOverlayType DamageOverlayType = DamageOverlayType.HUMAN; //what kind of damage overlays (if any) appear on our species when wounded?
	bool checkDeath = false;
	void Update(){
		if (mobStat == MobConsciousStat.UNCONSCIOUS && CustomNetworkManager.Instance._isServer && !checkDeath) {
			checkDeath = true;
			StartCoroutine(WaitForCritUpdate());
			Debug.Log("Do kill for demo");
		}
	}

	IEnumerator WaitForCritUpdate(){
	
		yield return new WaitForSeconds(4f);
		if(mobStat != MobConsciousStat.DEAD)
		Death(false);
	}
    // Use this for initialization
	public override void Death(bool gibbed){
		
		if (CustomNetworkManager.Instance._isServer) {
				PlayerNetworkActions pNet = GetComponent<PlayerNetworkActions>();
				pNet.RpcSpawnGhost();
			    
				PlayerMove pM = GetComponent<PlayerMove>();
				pM.isGhost = true;
				pM.allowInput = true;
			if(lastDamager != gameObject.name){
				PlayerList.Instance.UpdateKillScore(lastDamager);
				pNet.CmdSendAllertMessage("<color=red><b>"+lastDamager+"</b> has killed <b>"+gameObject.name+"</b></color>", true); //killfeed
			} else{
				pNet.CmdSendAllertMessage("<color=red><b>"+gameObject.name+" commited suicide</b></color>", true); //killfeed
			}

		}
		mobStat = MobConsciousStat.DEAD;
		base.Death(gibbed);
	}


}
