using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapDamage : MonoBehaviour
{
	private TileChangeManager tileChangeManager;

	private MetaDataLayer metaDataLayer;
	private MetaTileMap metaTileMap;

	public Layer Layer { get; private set; }

	//FIXME: cache construction prefabs in CraftingManager.Construction:
	private GameObject glassShardPrefab;
	private GameObject rodsPrefab;

	void Awake()
	{
		tileChangeManager = transform.GetComponentInParent<TileChangeManager>();
		metaDataLayer = transform.GetComponentInParent<MetaDataLayer>();
		metaTileMap = transform.GetComponentInParent<MetaTileMap>();

		Layer = GetComponent<Layer>();
	}

	void Start()
	{
		glassShardPrefab = Resources.Load("GlassShard") as GameObject;
		rodsPrefab = Resources.Load("Rods") as GameObject;
	}

	//Server Only:
	public void OnCollisionEnter2D(Collision2D coll)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			return;
		}

		ContactPoint2D firstContact = coll.GetContact(0);
		Vector2 dirOfForce = (firstContact.point - (Vector2) coll.transform.position).normalized;
		DetermineAction(coll.gameObject, dirOfForce);
	}

	private void DetermineAction(GameObject objectColliding, Vector2 forceDirection)
	{
		BulletBehaviour bulletBehaviour = objectColliding.GetComponent<BulletBehaviour>();
		if (bulletBehaviour != null)
		{
			DoBulletDamage(bulletBehaviour, forceDirection);
		}
	}

	private void DoBulletDamage(BulletBehaviour bullet, Vector3 forceDir)
	{
		forceDir.z = 0;
		Vector3 bulletHitTarget = bullet.transform.position + (forceDir * 0.2f);
		Vector3Int cellPos = Vector3Int.RoundToInt(transform.InverseTransformPoint(bulletHitTarget));
		MetaDataNode data = metaDataLayer.Get(cellPos);

		if (Layer.LayerType == LayerType.Windows)
		{
			LayerTile getTile = metaTileMap.GetTile(cellPos, LayerType.Windows);
			if (getTile != null)
			{
				//TODO damage amt based off type of bullet
				AddWindowDamage(bullet.damage, data, cellPos, bulletHitTarget);
				return;
			}
		}

		if (Layer.LayerType == LayerType.Grills)
		{
			//Make sure a window is not protecting it first:
			if (!metaTileMap.HasTile(cellPos, LayerType.Windows))
			{
				if (metaTileMap.HasTile(cellPos, LayerType.Grills))
				{
					//TODO damage amt based off type of bullet
					AddGrillDamage(bullet.damage, data, cellPos, bulletHitTarget);
				}
			}
		}
	}

	public void DoThrowDamage(Vector3Int worldTargetPos, ThrowInfo throwInfo, int dmgAmt)
	{
		DoMeleeDamage(new Vector2(worldTargetPos.x, worldTargetPos.y), throwInfo.ThrownBy, dmgAmt);
	}

	//Only works serverside:
	public void DoMeleeDamage(Vector2 dmgPosition, GameObject originator, int dmgAmt)
	{
		Vector3Int cellPos = metaTileMap.WorldToCell(dmgPosition);
		MetaDataNode data = metaDataLayer.Get(cellPos);

		if (Layer.LayerType == LayerType.Windows)
		{
			if (metaTileMap.HasTile(cellPos, LayerType.Windows))
			{
				PlaySoundMessage.SendToAll("GlassHit", dmgPosition, Random.Range(0.9f, 1.1f));
				AddWindowDamage(dmgAmt, data, cellPos, dmgPosition);
				return;
			}
		}

		if (Layer.LayerType == LayerType.Grills)
		{
			//Make sure a window is not protecting it first:
			if (!metaTileMap.HasTile(cellPos, LayerType.Windows))
			{
				if (metaTileMap.HasTile(cellPos, LayerType.Grills))
				{
					PlaySoundMessage.SendToAll("GrillHit", dmgPosition, Random.Range(0.9f, 1.1f));
					AddGrillDamage(dmgAmt, data, cellPos, dmgPosition);
				}
			}
		}
	}

	private void AddWindowDamage(int damage, MetaDataNode data, Vector3Int cellPos, Vector3 bulletHitTarget)
	{
		data.Damage += damage;
		if (data.Damage >= 20 && data.Damage < 50 && data.WindowDmgType != "crack01")
		{
			tileChangeManager.UpdateTile(cellPos, TileType.Damaged, "crack01");
			data.WindowDmgType = "crack01";
		}

		if (data.Damage >= 50 && data.Damage < 75 && data.WindowDmgType != "crack02")
		{
			tileChangeManager.UpdateTile(cellPos, TileType.Damaged, "crack02");
			data.WindowDmgType = "crack02";
		}

		if (data.Damage >= 75 && data.Damage < 100 && data.WindowDmgType != "crack03")
		{
			tileChangeManager.UpdateTile(cellPos, TileType.Damaged, "crack03");
			data.WindowDmgType = "crack03";
		}

		if (data.Damage >= 100 && data.WindowDmgType != "broken")
		{
			tileChangeManager.RemoveTile(cellPos, LayerType.Windows);

			//Spawn 3 glass shards with different sprites:
			SpawnGlassShards(bulletHitTarget);

			//Play the breaking window sfx:
			PlaySoundMessage.SendToAll("GlassBreak0" + Random.Range(1, 4).ToString(), bulletHitTarget, 1f);

			data.WindowDmgType = "broken";
			data.ResetDamage();
		}
	}

	private void AddGrillDamage(int damage, MetaDataNode data, Vector3Int cellPos, Vector3 bulletHitTarget)
	{
		data.Damage += damage;

		//Make grills a little bit weaker (set to 60 hp):
		if (data.Damage >= 60)
		{
			tileChangeManager.RemoveTile(cellPos, LayerType.Grills);
			tileChangeManager.UpdateTile(cellPos, TileType.Damaged, "GrillDestroyed");

			PlaySoundMessage.SendToAll("GrillHit", bulletHitTarget, 1f);

			//Spawn rods:
			SpawnRods(bulletHitTarget);

			data.ResetDamage();
		}
	}

	//Only works server side:
	public void WireCutGrill(Vector3 snipPosition)
	{
		Vector3Int cellPos = metaTileMap.WorldToCell(snipPosition);
		MetaDataNode data = metaDataLayer.Get(cellPos);

		if (Layer.LayerType == LayerType.Grills)
		{
			//Make sure a window is not protecting it first:
			if (!metaTileMap.HasTile(cellPos, LayerType.Windows))
			{
				if (metaTileMap.HasTile(cellPos, LayerType.Grills))
				{
					tileChangeManager.RemoveTile(cellPos, LayerType.Grills);

					PlaySoundMessage.SendToAll("WireCutter", snipPosition, 1f);
					SpawnRods(snipPosition);
				}
			}
		}

		data.ResetDamage();
	}

	private void SpawnRods(Vector3 pos)
	{
		GameObject rods = PoolManager.Instance.PoolNetworkInstantiate(rodsPrefab, Vector3Int.RoundToInt(pos),
			Quaternion.identity);

		CustomNetTransform netTransform = rods.GetComponent<CustomNetTransform>();
		netTransform?.SetPosition(netTransform.ServerState.WorldPosition + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)));
	}

	private void SpawnGlassShards(Vector3 pos)
	{
		//Spawn 3 glass shards with different sprites:
		PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(pos),
			Quaternion.identity).GetComponent<GlassShard>().SetSpriteAndScatter(0);

		PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(pos),
			Quaternion.identity).GetComponent<GlassShard>().SetSpriteAndScatter(1);

		PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(pos),
			Quaternion.identity).GetComponent<GlassShard>().SetSpriteAndScatter(2);

		//Play the breaking window sfx:
		PlaySoundMessage.SendToAll("GlassBreak0" + Random.Range(1, 4), pos, 1f);
	}
}