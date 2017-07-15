using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using UI;

public class Human : Carbon
{
	private float respawnTime = 0f;
	private bool countRespawn = false;
    // See species.dm for human
    public DamageOverlayType DamageOverlayType = DamageOverlayType.HUMAN; //what kind of damage overlays (if any) appear on our species when wounded?
    bool checkDeath = false;
    void Update()
    {
        if (mobStat == MobConsciousStat.UNCONSCIOUS && CustomNetworkManager.Instance._isServer && !checkDeath)
        {
            checkDeath = true;
            StartCoroutine(WaitForCritUpdate());
            Debug.Log("Do kill for demo");
        }

		if (countRespawn) {
			respawnTime += Time.deltaTime;
			if (respawnTime >= 10f) {
				countRespawn = false;
				GetComponent<PlayerNetworkActions>().RespawnPlayer();
			}
		}
    }

    IEnumerator WaitForCritUpdate()
    {
        yield return new WaitForSeconds(4f);
        if (mobStat != MobConsciousStat.DEAD)
            Death(false);
    }



    public override void Death(bool gibbed)
    {

        if (CustomNetworkManager.Instance._isServer)
        {
            PlayerNetworkActions pNet = GetComponent<PlayerNetworkActions>();
            pNet.RpcSpawnGhost();

            PlayerMove pM = GetComponent<PlayerMove>();
            pM.isGhost = true;
            pM.allowInput = true;
            if (lastDamager != gameObject.name)
            {
                PlayerList.Instance.UpdateKillScore(lastDamager);
                pNet.CmdSendAlertMessage("<color=red><b>" + lastDamager + "</b> has killed <b>" + gameObject.name + "</b></color>", true); //killfeed
            }
            else
            {
                pNet.CmdSendAlertMessage("<color=red><b>" + gameObject.name + " commited suicide</b></color>", true); //killfeed
            }
			countRespawn = true;
			respawnTime = 0f;
			GetComponent<PlayerNetworkActions>().CmdDropItem("leftHand");
			GetComponent<PlayerNetworkActions>().CmdDropItem("rightHand");
			Debug.Log("respawn initiated..");
        }
        mobStat = MobConsciousStat.DEAD;
        base.Death(gibbed);
    }
		
    public override void RightClickContextMenu()
    {
        // TODO: Apply Context Menu Here
        // For now just examine
        UIManager.Chat.AddChatEvent(new ChatEvent("This is " + this.name + ", a " + this.GetType().Name + "\r\n"));

        // TODO: Read from item in inventory here for the cards name and job title
    }
}