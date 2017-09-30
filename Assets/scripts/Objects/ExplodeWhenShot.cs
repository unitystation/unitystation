using System;
using System.Collections;
using System.Collections.Generic;
using Light2D;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;
using Random = UnityEngine.Random;

public class ExplodeWhenShot : NetworkBehaviour
{
    public int damage = 150;
    public float radius = 3f;

    const int MAX_TARGETS = 44;

    readonly string[] explosions = {"Explosion1", "Explosion2"};
    readonly Collider2D[] colliders = new Collider2D[MAX_TARGETS];

    int playerMask;
    int damageableMask;
    int obstacleMask;
    private bool hasExploded = false;

    private GameObject lightFxInstance;
    private LightSprite lightSprite;
    public SpriteRenderer spriteRend;

    void Start()
    {
        playerMask = LayerMask.GetMask( "Players" );
        damageableMask = LayerMask.GetMask( "Players", "Machines", "Default" /*, "Lighting", "Items"*/ );
        obstacleMask = LayerMask.GetMask( "Walls", "Door Closed" );
    }

//	void OnTriggerEnter2D(Collider2D coll)
//	{
//		if (hasExploded)
//			return;
//		
//		var bullet = coll.GetComponent<BulletBehaviour>();
//		if (bullet != null) {
//			if (isServer) {
//				Explode(bullet.shooterName);
//			}
//			hasExploded = true;
//			GoBoom();
//			PoolManager.PoolClientDestroy(bullet.gameObject);
//		}
//	}

//#if !ENABLE_PLAYMODE_TESTS_RUNNER
//	[Server]
//	#endif
    public void ExplodeOnDamage( string damagedBy )
    {
        if (hasExploded)
            return;
//        Debug.Log("Exploding on damage!");
        if ( isServer )
        {
            Explode( damagedBy ); //fixme
        }
        hasExploded = true;
        GoBoom();
    }

#if !ENABLE_PLAYMODE_TESTS_RUNNER
	[Server]
	#endif
    public void Explode( string thanksTo )
    {
        var explosionPos = (Vector2) transform.position;
        var length = Physics2D.OverlapCircleNonAlloc( explosionPos, radius, colliders, damageableMask );
        Dictionary<GameObject, int> toBeDamaged = new Dictionary<GameObject, int>();
        for ( int i = 0; i < length; i++ )
        {
            var localCollider = colliders[i];
            var localObject = localCollider.gameObject;
//			var living = collider.gameObject.GetComponent<Living>();
//			if (living != null) {
//				var livingPos = (Vector2)living.transform.position;
//				var distance = Vector3.Distance(pos, livingPos);
//
//				var hit = Physics2D.Raycast(pos, livingPos - pos, distance, obstacleMask);
//				if (hit.collider == null) {
//					var effect = 1 - ((distance * distance) / (radius * radius));
//					var actualDamage = (int)(damage * effect);
//					HurtPeople(living, bulletOwnedBy, actualDamage);
//				}
//			}
            var localObjectPos = (Vector2) localObject.transform.position;
            var distance = Vector3.Distance( explosionPos, localObjectPos );
            var effect = 1 - ( ( distance * distance ) / ( radius * radius ) );
            var actualDamage = (int) ( damage * effect );
//            Debug.LogFormat("{0} distance = {1}", localObject, distance);
            if ( NotSameObject( localCollider ) &&
                 HasHealthComponent( localCollider ) &&
                 IsWithinReach( explosionPos, localObjectPos, distance ) &&
                 HasEffectiveDamage( actualDamage ) //todo check why it's reaching negative values anyway
            )
            {
                toBeDamaged[localObject] = actualDamage;
            }
        }
//        Debug.LogFormat( "toBeDamaged.size={0}", toBeDamaged.Count );
        foreach ( var pair in toBeDamaged )
        {
//            Debug.LogFormat( "{0} damaged {1} with {2} on explosion!", gameObject.name, pair.Key.name, pair.Value );
            pair.Key.GetComponent<HealthBehaviour>()
                .ApplyDamage( String.Format( "{0} – {1}", gameObject.name, thanksTo ), pair.Value, DamageType.BURN );
        }
        RpcClientExplode();
        StartCoroutine(WaitToDestroy());
    }

    [ClientRpc]
    void RpcClientExplode(){
        if (!hasExploded)
        {
            hasExploded = true;
            GoBoom();
        }
    }

    IEnumerator WaitToDestroy(){
        yield return new WaitForSeconds(5f);
        NetworkServer.Destroy(gameObject);
    }

    private bool HasEffectiveDamage( int actualDamage )
    {
        return actualDamage > 0;
    }

    private bool IsWithinReach( Vector2 pos, Vector2 damageablePos, float distance )
    {
        return distance <= radius
               &&
               Physics2D.Raycast( pos, damageablePos - pos, distance, obstacleMask ).collider == null;
    }

    private static bool HasHealthComponent( Collider2D localCollider )
    {
        return localCollider.gameObject.GetComponent<HealthBehaviour>() != null;
    }

    private bool NotSameObject( Collider2D localCollider )
    {
        return !localCollider.gameObject.Equals( gameObject );
    }

    internal virtual void GoBoom()
    {
        if (spriteRend.isVisible)
            Camera2DFollow.followControl.Shake(0.2f, 0.2f);
        // Instantiate a clone of the source so that multiple explosions can play at the same time.
        spriteRend.enabled = false;
        try{
            Matrix.Matrix.At(transform.position).TryRemoveTile(gameObject);
            PushPull oA = gameObject.GetComponent<PushPull>();
            if(oA != null){
                if(oA.pusher == PlayerManager.LocalPlayer){
                    PlayerManager.LocalPlayerScript.playerMove.isPushing = false;
                }
                oA.isPushable = false;
            }
        } catch {
            Debug.LogWarning("Object may of already been removed");
        }
        Collider2D[] getColls = gameObject.GetComponents<Collider2D>();
        for (int i = 0; i < getColls.Length; i++)
        {
            getColls[i].enabled = false;
        }
        var name = explosions[Random.Range( 0, explosions.Length )];
        var source = SoundManager.Instance[name];
        if ( source != null )
        {
            Instantiate<AudioSource>( source, transform.position, Quaternion.identity ).Play();
        }

        var fireRing = Resources.Load<GameObject>( "effects/FireRing" );
        Instantiate( fireRing, transform.position, Quaternion.identity );

        var lightFx = Resources.Load<GameObject>( "lighting/BoomLight" );
        lightFxInstance = Instantiate( lightFx, transform.position, Quaternion.identity );
        lightSprite = lightFxInstance.GetComponentInChildren<LightSprite>();
        lightSprite.fadeFX( 1f );
        SetFire();
    }

    void SetFire()
    {
        int maxNumOfFire = 4;
        int cLength = 3;
        int rHeight = 3;
        var pos = (Vector2) transform.position;
        EffectsFactory.Instance.SpawnFileTile( Random.Range( 0.4f, 1f ), pos );
        pos.x -= 1f;
        pos.y += 1f;

        for ( int i = 0; i < cLength; i++ )
        {
            for ( int j = 0; j < rHeight; j++ )
            {
                if ( j == 0 && i == 0 || j == 2 && i == 0 || j == 2 && i == 2 )
                    continue;

                Vector2 checkPos = new Vector2( pos.x + (float) i, pos.y - (float) j );
                if ( Matrix.Matrix.At( checkPos ).IsPassable() || Matrix.Matrix.At( checkPos ).IsPlayer() )
                {
                    EffectsFactory.Instance.SpawnFileTile( Random.Range( 0.4f, 1f ), checkPos );
                    maxNumOfFire--;
                }
                if ( maxNumOfFire <= 0 )
                {
                    break;
                }
            }
        }
    }

//    [Obsolete]
//    internal virtual void HurtPeople( Living living, string damagedBy, int damage )
//    {
//        living.RpcReceiveDamage( damagedBy, damage, DamageType.BURN, BodyPartType.CHEST );
//    }
}