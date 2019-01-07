using System.Collections;
using System.Collections.Generic;
using Light2D;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Behavior for an object which explodes when it is damaged
/// </summary>
public class ExplodeWhenShot : NetworkBehaviour
{
	//explosion damage
	public int damage = 150;
	//explosion radius in tiles
	public float radius = 3f;
	//sprite renderer to use for the explosion
	public SpriteRenderer spriteRend;

	//max number of things an explosion can hit
	private const int MAX_TARGETS = 44;
	private readonly string[] EXPLOSION_SOUNDS = { "Explosion1", "Explosion2" };
	//LayerMask for things that can be damaged
	private int DAMAGEABLE_MASK;
	//LayerMask for obstructions which can block the explosion
	private int OBSTACLE_MASK;
	//collider array to re-use when checking for collisions with the explosion
	private readonly Collider2D[] colliders = new Collider2D[MAX_TARGETS];

	//whether this object has exploded
	private bool hasExploded;	
	//this object's registerObject
	private RegisterObject registerObject;
	//Temporary game object created during the explosion
	private GameObject lightFxInstance;
	//this object's custom net transform
	private CustomNetTransform customNetTransform;


	private void Start()
	{
		DAMAGEABLE_MASK = LayerMask.GetMask("Players", "Machines", "Default" /*, "Lighting", "Items"*/);
		OBSTACLE_MASK = LayerMask.GetMask("Walls", "Door Closed");

		registerObject = GetComponent<RegisterObject>();
		customNetTransform = GetComponent<CustomNetTransform>();
	}

	public void ExplodeOnDamage(string damagedBy)
	{
		if (hasExploded)
		{
			return;
		}
		//        Logger.Log("Exploding on damage!");
		if (isServer)
		{
			CalcAndApplyExplosionDamage(damagedBy); //fixme
			RpcClientExplode();
			StartCoroutine(WaitToDestroy());
		}
		hasExploded = true;
		DisplayExplosion();
	}

	/// <summary>
	/// Calculate and apply the damage that should be caused by the explosion, updating the server's state for the damaged
	/// objects.
	/// </summary>
	/// <param name="thanksTo">string of the entity that caused the explosion</param>
	[Server]
	public void CalcAndApplyExplosionDamage(string thanksTo)
	{
		//NOTE: There is no need for this method to be public except for unit testing.
		Vector2 explosionPos = transform.position;
		int length = Physics2D.OverlapCircleNonAlloc(explosionPos, radius, colliders, DAMAGEABLE_MASK);
		Dictionary<GameObject, int> toBeDamaged = new Dictionary<GameObject, int>();
		for (int i = 0; i < length; i++)
		{
			Collider2D localCollider = colliders[i];
			GameObject localObject = localCollider.gameObject;

			Vector2 localObjectPos = localObject.transform.position;
			float distance = Vector3.Distance(explosionPos, localObjectPos);
			float effect = 1 - distance * distance / (radius * radius);
			int actualDamage = (int)(damage * effect);

			if (NotSameObject(localCollider) && HasHealthComponent(localCollider) && IsWithinReach(explosionPos, localObjectPos, distance) &&
				HasEffectiveDamage(actualDamage) //todo check why it's reaching negative values anyway
			)
			{
				toBeDamaged[localObject] = actualDamage;
			}
		}

		foreach (KeyValuePair<GameObject, int> pair in toBeDamaged)
		{
			pair.Key.GetComponent<LivingHealthBehaviour>()
				.ApplyDamage(pair.Key, pair.Value, DamageType.Burn);
		}
		RpcClientExplode();
		gameObject.GetComponent<ObjectBehaviour>().visibleState = false;
		StartCoroutine(WaitToDestroy());
	}

	/// <summary>
	/// Updates each client, telling them to display the explosion effect on their own version
	/// of the object and make the object vanish due to the explosion.
	/// </summary>
	[ClientRpc]
	private void RpcClientExplode()
	{
		if (!hasExploded)
		{
			hasExploded = true;
			DisplayExplosion();
		}
	}

	/// <summary>
	/// Destroy the exploded game object (removing it completely from the game) after a few seconds.
	/// </summary>
	/// <returns></returns>
	private IEnumerator WaitToDestroy()
	{
		yield return new WaitForSeconds(5f);
		NetworkServer.Destroy(gameObject);
	}

	private bool HasEffectiveDamage(int actualDamage)
	{
		return actualDamage > 0;
	}

	private bool IsWithinReach(Vector2 pos, Vector2 damageablePos, float distance)
	{
		return distance <= radius && Physics2D.Raycast(pos, damageablePos - pos, distance, OBSTACLE_MASK).collider == null;
	}

	private static bool HasHealthComponent(Collider2D localCollider)
	{
		return localCollider.gameObject.GetComponent<LivingHealthBehaviour>() != null;
	}

	private bool NotSameObject(Collider2D localCollider)
	{
		return !localCollider.gameObject.Equals(gameObject);
	}

	/// <summary>
	/// Handles the visual effect of the explosion / disappearing of the object
	/// </summary>
	internal virtual void DisplayExplosion()
	{
		//NOTE: This runs on both the client and the server.
		//When something goes boom on a client, the client makes its own version go boom,
		// the server makes its own version of the object go boom and
		//sends an RPC to all the clients to tell them to make their version of the object go boom as well. So this
		//method ends up being invoked on clients and server.

		
		//Shake if the player is on the same matrix (check for null in case this is a headless server)
		if (PlayerManager.LocalPlayer != null &&
			PlayerManager.LocalPlayer.gameObject.GetComponent<RegisterPlayer>() == registerObject.Matrix)
		{
			Camera2DFollow.followControl.Shake(0.2f, 0.2f);
		}

		// Instantiate a clone of the source so that multiple explosions can play at the same time.
		string name = EXPLOSION_SOUNDS[Random.Range(0, EXPLOSION_SOUNDS.Length)];
		AudioSource source = SoundManager.Instance[name];
		if (source != null)
		{
			Instantiate(source, transform.position, Quaternion.identity).Play();
		}

		GameObject fireRing = Resources.Load<GameObject>("effects/FireRing");
		Instantiate(fireRing, transform.position, Quaternion.identity);

		GameObject lightFx = Resources.Load<GameObject>("lighting/BoomLight");
		lightFxInstance = Instantiate(lightFx, transform.position, Quaternion.identity);
		//LightSprite lightSprite = lightFxInstance.GetComponentInChildren<LightSprite>();
		//lightSprite.fadeFX(1f); // TODO Removed animation (Should be in a separate component)
		SetFire();

		//make the actual tank disappear
		DisappearObject();
	}

	/// <summary>
	/// disappear this object (while still keeping the explosion around)
	/// </summary>
	private void DisappearObject()
	{
		//NOTE: This runs on both the client and the server. When it runs on the server,
		//we need to make sure the server knows it should be disappeared. When it runs on the
		//client we need to make the clients local version of the object disappear
		if (isServer)
		{
			//make it vanish in the server's state of the world
			customNetTransform.DisappearFromWorldServer();
		}
		else
		{
			//make it vanish in the client's local world
			customNetTransform.DisappearFromWorld();
		}		
	}

	private void SetFire()
	{
		int maxNumOfFire = 4;
		int cLength = 3;
		int rHeight = 3;
		Vector3Int pos = Vector3Int.RoundToInt(transform.localPosition);
		EffectsFactory.Instance.SpawnFileTileLocal(Random.Range(0.4f, 1f), pos, transform.parent);
		pos.x--;
		pos.y++;

		for (int i = 0; i < cLength; i++)
		{
			for (int j = 0; j < rHeight; j++)
			{
				if (j == 0 && i == 0 || j == 2 && i == 0 || j == 2 && i == 2)
				{
					continue;
				}

				Vector3Int checkPos = new Vector3Int(pos.x + i, pos.y - j, 0);
				if (registerObject.Matrix.IsPassableAt(checkPos)) // || MatrixOld.Matrix.At(checkPos).IsPlayer())
				{
					EffectsFactory.Instance.SpawnFileTileLocal(Random.Range(0.4f, 1f), checkPos, transform.parent);
					maxNumOfFire--;
				}
				if (maxNumOfFire <= 0)
				{
					break;
				}
			}
		}
	}
}