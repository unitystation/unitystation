using System.Collections;
using System.Collections.Generic;
using Light2D;
using PlayGroup;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts;
using UnityEngine;
using UnityEngine.Networking;

public class ExplodeWhenShot : NetworkBehaviour
{
	public int damage = 150;
	public float radius = 3f;

	private const int MAX_TARGETS = 44;

	private readonly string[] explosions = {"Explosion1", "Explosion2"};
	private readonly Collider2D[] colliders = new Collider2D[MAX_TARGETS];

	private int playerMask;
	private int damageableMask;
	private int obstacleMask;
	private bool hasExploded;

	private GameObject lightFxInstance;
	private LightSprite lightSprite;
	public SpriteRenderer spriteRend;

	private RegisterTile registerTile;
	private Matrix matrix => registerTile.Matrix;

	private void Start()
	{
		playerMask = LayerMask.GetMask("Players");
		damageableMask = LayerMask.GetMask("Players", "Machines", "Default" /*, "Lighting", "Items"*/);
		obstacleMask = LayerMask.GetMask("Walls", "Door Closed");

		registerTile = GetComponent<RegisterTile>();
	}

	//#if !ENABLE_PLAYMODE_TESTS_RUNNER
	//	[Server]
	//	#endif
	public void ExplodeOnDamage(string damagedBy)
	{
		if (hasExploded)
		{
			return;
		}
		//        Debug.Log("Exploding on damage!");
		if (isServer)
		{
			Explode(damagedBy); //fixme
		}
		hasExploded = true;
		GoBoom();
	}

#if !ENABLE_PLAYMODE_TESTS_RUNNER
	[Server]
#endif
	public void Explode(string thanksTo)
	{
		Vector2 explosionPos = transform.position;
		int length = Physics2D.OverlapCircleNonAlloc(explosionPos, radius, colliders, damageableMask);
		Dictionary<GameObject, int> toBeDamaged = new Dictionary<GameObject, int>();
		for (int i = 0; i < length; i++)
		{
			Collider2D localCollider = colliders[i];
			GameObject localObject = localCollider.gameObject;

			Vector2 localObjectPos = localObject.transform.position;
			float distance = Vector3.Distance(explosionPos, localObjectPos);
			float effect = 1 - distance * distance / (radius * radius);
			int actualDamage = (int) (damage * effect);

			if (NotSameObject(localCollider) && HasHealthComponent(localCollider) && IsWithinReach(explosionPos, localObjectPos, distance) &&
			    HasEffectiveDamage(actualDamage) //todo check why it's reaching negative values anyway
			)
			{
				toBeDamaged[localObject] = actualDamage;
			}
		}

		foreach (KeyValuePair<GameObject, int> pair in toBeDamaged)
		{
			pair.Key.GetComponent<HealthBehaviour>()
				.ApplyDamage(pair.Key, pair.Value, DamageType.BURN);
		}
		RpcClientExplode();
		StartCoroutine(WaitToDestroy());
	}

	[ClientRpc]
	private void RpcClientExplode()
	{
		if (!hasExploded)
		{
			hasExploded = true;
			GoBoom();
		}
	}

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
		return distance <= radius && Physics2D.Raycast(pos, damageablePos - pos, distance, obstacleMask).collider == null;
	}

	private static bool HasHealthComponent(Collider2D localCollider)
	{
		return localCollider.gameObject.GetComponent<HealthBehaviour>() != null;
	}

	private bool NotSameObject(Collider2D localCollider)
	{
		return !localCollider.gameObject.Equals(gameObject);
	}

	internal virtual void GoBoom()
	{
		if (spriteRend.isVisible)
		{
			Camera2DFollow.followControl.Shake(0.2f, 0.2f);
		}
		// Instantiate a clone of the source so that multiple explosions can play at the same time.
		spriteRend.enabled = false;
		try
		{
			registerTile.Unregister();

			PushPull oA = gameObject.GetComponent<PushPull>();
			if (oA != null)
			{
				if (oA.pusher == PlayerManager.LocalPlayer)
				{
					PlayerManager.LocalPlayerScript.playerMove.IsPushing = false;
				}
				oA.isPushable = false;
			}
		}
		catch
		{
			Debug.LogWarning("Object may of already been removed");
		}

		foreach (Collider2D collider2d in gameObject.GetComponents<Collider2D>())
		{
			collider2d.enabled = false;
		}

		string name = explosions[Random.Range(0, explosions.Length)];
		AudioSource source = SoundManager.Instance[name];
		if (source != null)
		{
			Instantiate(source, transform.position, Quaternion.identity).Play();
		}

		GameObject fireRing = Resources.Load<GameObject>("effects/FireRing");
		Instantiate(fireRing, transform.position, Quaternion.identity);

		GameObject lightFx = Resources.Load<GameObject>("lighting/BoomLight");
		lightFxInstance = Instantiate(lightFx, transform.position, Quaternion.identity);
		lightSprite = lightFxInstance.GetComponentInChildren<LightSprite>();
		lightSprite.fadeFX(1f);
		SetFire();
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
				if (matrix.IsPassableAt(checkPos)) // || MatrixOld.Matrix.At(checkPos).IsPlayer())
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