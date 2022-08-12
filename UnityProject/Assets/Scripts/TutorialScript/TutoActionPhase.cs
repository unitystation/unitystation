using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutoActionPhase : Tutorial
{
    public GameObject tutoBot;
    public Transform spawnPoint;
    public Tutorial tutoParent;
    
    ///change phase + send message
    void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider.gameObject.layer == 8)
        {
            tutoParent.tutoPhase = this.tutoPhase;
            
            if(tutoPhase != Phase.SpawnMove)
            {
                Message(Tutorial.botGO);
            }
        }
    }

    ///starting phase
    private void OnTriggerStay2D(Collider2D collider)
    {
        if(collider.gameObject.layer == 8 && tutoPhase == Phase.SpawnMove)
        {
            SpawnTutoBot();
        }
    }

    ///SPAWN PHASE
    private void SpawnTutoBot()
    {
        //Spawn tuto bot
        SpawnResult bot = Spawn.ServerPrefab(tutoBot, spawnPoint.position, null, Quaternion.identity);
        Tutorial.botGO = bot.GameObject;
        Tutorial.botGO.GetComponent<Systems.MobAIs.MobFollow>().StartFollowing(PlayerList.Instance.InGamePlayers[0].GameObject);
        Tutorial.botGO.GetComponent<TutoBot>().tuto = tutoParent;
        GameObject GO1 = GameObject.Find("NetworkTabs (Top Right windows)");
        GameObject GO2 = GameObject.Find("AdminUI");
        Debug.Log(GO1);
        Debug.Log(GO2);
        GO1.SetActive(false);
        GO2.SetActive(false);

        Message(Tutorial.botGO);
        if(this.deleteGO)
            Destroy(this.gameObject);
    }
}
