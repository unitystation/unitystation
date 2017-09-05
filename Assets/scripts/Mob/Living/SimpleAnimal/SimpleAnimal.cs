using NPC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Networking;

public class SimpleAnimal : Living
{

    // Inspector Properties
    public bool sliced = false;
    private SpriteRenderer spriteRenderer;
    private RandomMove randomMove;

    // simple_animal.dm var/icon_dead
    public Sprite deadSprite;

    // simple_animal.dm var/icon_gib
    public GameObject gibPrefab;

    // Use this for initialization
    void Start()
    {
        maxHealth = InitialMaxHealth;
        UpdateHealth();

        spriteRenderer = GetComponent<SpriteRenderer>();
        randomMove = GetComponent<RandomMove>();

    }
		
    #region simple_animal.dm

    // see simple_animal.dm /mob/living/simple_animal/updatehealth()
    public override void UpdateHealth()
    {
        base.UpdateHealth();
        health = DMMath.Clamp(health, 0, maxHealth);
    }

    // see /mob/living/simple_animal/update_stat()
    public override void UpdateStat()
    {
        if ((StatusFlags & MobStatusFlag.GODMODE) != 0)
            return;

        if (mobStat != ConsciousState.DEAD)
        {
            if (health <= 0)
                Death();
            else
                mobStat = ConsciousState.CONSCIOUS;
        }
    }

    // see /mob/living/simple_animal/death(gibbed)
    public override void Death(bool gibbed = false)
    {
        if (!gibbed)
        {
            SoundManager.Play("Bodyfall", 0.5f);
        }

        health = 0;
        randomMove.enabled = false;
        spriteRenderer.sprite = deadSprite;
        base.Death(gibbed);
        
    }

	[Server]
    public override void GibAnimation()
    {
        GameObject corpse = Instantiate(gibPrefab, transform.position, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(corpse);
    }

    #endregion
}
