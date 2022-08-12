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
            else
            {
                SpawnTutoBot();
            }
        }
    }

    ///SPAWN PHASE
    private void SpawnTutoBot()
    {
        //Spawn tuto bot
        if(GameObject.Find("TutoBot") != null)
            Destroy(this.gameObject);
        else
        {
            SpawnResult bot = Spawn.ServerPrefab(tutoBot, spawnPoint.position, null, Quaternion.identity);
            Tutorial.botGO = bot.GameObject;
            Tutorial.botGO.GetComponent<Systems.MobAIs.MobFollow>().StartFollowing(PlayerList.Instance.InGamePlayers[0].GameObject);
            Tutorial.botGO.GetComponent<TutoBot>().tuto = tutoParent;
            this.Message(bot.GameObject);
            GameObject GO1 = GameObject.Find("NetworkTabs (Top Right windows)");
            GameObject GO2 = GameObject.Find("AdminUI");
            GO1.SetActive(false);
            GO2.SetActive(false);
        }

        if(this.deleteGO)
            Destroy(this.gameObject);
    }
}
