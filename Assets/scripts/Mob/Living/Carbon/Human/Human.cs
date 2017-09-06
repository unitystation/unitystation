using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayGroup;
using UI;
[Obsolete]
public class Human : Carbon
{
//	private float respawnTime = 0f;
//	private bool countRespawn = false;
//    // See species.dm for human
//    public DamageOverlayType DamageOverlayType = DamageOverlayType.HUMAN; //what kind of damage overlays (if any) appear on our species when wounded?
//    bool checkDeath = false;
//    void Update()
//    {
//        if (mobStat == ConsciousState.UNCONSCIOUS && CustomNetworkManager.Instance._isServer && !checkDeath)
//        {
//            checkDeath = true;
//            StartCoroutine(WaitForCritUpdate());
//            Debug.Log("Do kill for demo");
//        }
//
//		if (countRespawn) {
//			respawnTime += Time.deltaTime;
//			if (respawnTime >= 10f) {
//				countRespawn = false;
//				GetComponent<PlayerNetworkActions>().RespawnPlayer();
//			}
//		}
//    }
//
//    IEnumerator WaitForCritUpdate()
//    {
//        yield return new WaitForSeconds(4f);
//        if (mobStat != ConsciousState.DEAD)
//            Death(false);
//    }
//
//
//
//    public override void Death(bool gibbed = false)
//    {
//
//        if (CustomNetworkManager.Instance._isServer)
//        {
//            PlayerNetworkActions pNet = GetComponent<PlayerNetworkActions>();
//            pNet.RpcSpawnGhost();
//
//            PlayerMove pM = GetComponent<PlayerMove>();
//            pM.isGhost = true;
//            pM.allowInput = true;
//	        if ( lastDamager == gameObject.name )
//	        {
//		        pNet.CmdSendAlertMessage( "<color=red><b>" + gameObject.name + " commited suicide</b></color>",
//			        true ); //killfeed
//	        }
//	        else if(lastDamager.EndsWith( gameObject.name )) // chain reactions
//	        {
//		        pNet.CmdSendAlertMessage( "<color=red><b>" + gameObject.name + " screwed himself up with some help (" + 
//		                                  lastDamager
//		                                  + ")</b></color>",
//			        true ); //killfeed
//	        } 
//	        else 
//	        {
//		        PlayerList.Instance.UpdateKillScore( lastDamager );
//		        pNet.CmdSendAlertMessage(
//			        "<color=red><b>" + lastDamager + "</b> has killed <b>" + gameObject.name + "</b></color>", true ); //killfeed
//	        }
//	        countRespawn = true;
//			respawnTime = 0f;
//			GetComponent<PlayerNetworkActions>().CmdDropItem("leftHand");
//			GetComponent<PlayerNetworkActions>().CmdDropItem("rightHand");
////			gameObject.GetComponent<WeaponNetworkActions>().BloodSplat(transform.position, Sprites.BloodSplatSize.medium);
//			Debug.Log("respawn initiated..");
//        }
//        mobStat = ConsciousState.DEAD;
//        base.Death(gibbed);
//    }
//		
//    public override void RightClickContextMenu()
//    {
//        // TODO: Apply Context Menu Here
//        // For now just examine
//        UIManager.Chat.AddChatEvent(new ChatEvent("This is " + this.name + ", a " + this.GetType().Name + "\r\n"));
//
//        // TODO: Read from item in inventory here for the cards name and job title
//    }
}