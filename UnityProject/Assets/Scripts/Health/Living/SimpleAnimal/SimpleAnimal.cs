using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
using Sprites;

public class SimpleAnimal : HealthBehaviour
{
    [Header("For harvestable animals")]
    public GameObject[] butcherResults;

    public SpriteRenderer spriteRend;
    public Sprite aliveSprite;
    public Sprite deadSprite;

    //Syncvar hook so that new players can sync state on start
    [SyncVar(hook = "SetAliveState")]
    public bool deadState;

    void Start()
    {
        //Set it automatically because we are using the SimpleAnimalBehaviour
        isNPC = true;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        StartCoroutine(WaitForLoad());
    }

    IEnumerator WaitForLoad()
    {
        yield return new WaitForSeconds(2f);
        SetAliveState(deadState);
    }

    public override int ReceiveAndCalculateDamage(string damagedBy, int damage, DamageType damageType, BodyPartType bodyPartAim)
    {
        base.ReceiveAndCalculateDamage(damagedBy, damage, damageType, bodyPartAim);
        if (isServer)
            EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.medium);
        return damage;

    }

    [Server]
    public virtual void Harvest()
    {
        foreach (GameObject harvestPrefab in butcherResults)
        {
            GameObject harvest = Instantiate(harvestPrefab, transform.position, Quaternion.identity) as GameObject;
            NetworkServer.Spawn(harvest);
        }
        EffectsFactory.Instance.BloodSplat(transform.position, BloodSplatSize.medium);
        //Remove the NPC after all has been harvested
        NetworkServer.Destroy(gameObject);
    }

    [Server]
    protected override void OnDeathActions()
    {
        deadState = true;
    }

    void SetAliveState(bool state)
    {
        deadState = state;
        if (state)
        {
            spriteRend.sprite = deadSprite;
        }
        else
        {
            spriteRend.sprite = aliveSprite;
        }
    }
}