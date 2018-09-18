using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilemapDamage : MonoBehaviour
{
	private TileChangeManager tileChangeManager;
	private TileTrigger tileTrigger;

	private MetaDataLayer metaDataLayer;
	private MetaTileMap metaTileMap;

	private Layer thisLayer;

	//FIXME: cache construction prefabs in CraftingManager.Construction:
	private GameObject glassShardPrefab;
	private GameObject rodsPrefab;

	void Awake()
	{
		tileChangeManager = transform.root.GetComponent<TileChangeManager>();
		tileTrigger = transform.root.GetComponent<TileTrigger>();
		metaDataLayer = transform.parent.GetComponent<MetaDataLayer>();
		metaTileMap = transform.parent.GetComponent<MetaTileMap>();
		thisLayer = GetComponent<Layer>();
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
		var firstContact = coll.GetContact(0);
		var dirOfForce = (firstContact.point - (Vector2) coll.transform.position).normalized;
		DetermineAction(coll.gameObject, dirOfForce);
	}

	private void DetermineAction(GameObject objectColliding, Vector2 forceDirection)
	{

		var bulletBehaviour = objectColliding.GetComponent<BulletBehaviour>();
		if (bulletBehaviour != null)
		{
			DoBulletDamage(bulletBehaviour, forceDirection);
			return;
		}
	}

	private void DoBulletDamage(BulletBehaviour bullet, Vector3 forceDir)
	{
		forceDir.z = 0;
		var bulletHitTarget = bullet.transform.position + (forceDir * 0.5f);
		var cellPos = tileChangeManager.baseTileMap.WorldToCell(bulletHitTarget);
		var data = metaDataLayer.Get(cellPos);

		if (thisLayer.LayerType == LayerType.Windows)
		{
			var getTile = tileChangeManager.windowTileMap.GetTile(cellPos);
			if (getTile != null)
			{
				//TODO damage amt based off type of bullet
				AddWindowDamage(20, data, cellPos, bulletHitTarget);
				return;
			}
		}
		if (thisLayer.LayerType == LayerType.Objects)
		{
			var getWindowTile = tileChangeManager.windowTileMap.GetTile(cellPos);

			//Make sure a window is not protecting it first:
			if (!getWindowTile)
			{
				var getObjectTile = tileChangeManager.objectTileMap.GetTile(cellPos);
				if (getObjectTile != null)
				{
					//Check what type of tile it is from its name:
					//Debug.Log("TILE NAME: " + getObjectTile.name);

					//Do grill things:
					if (getObjectTile.name == "Grill")
					{
						//TODO damage amt based off type of bullet
						AddGrillDamage(20, data, cellPos, bulletHitTarget);
					}
				}
			}
		}
	}

	private void AddWindowDamage(int damage, MetaDataNode data, Vector3Int cellPos, Vector3 bulletHitTarget)
	{
		data.AddDamage(damage);
		if (data.GetDamage >= 20 && data.GetDamage < 50 && data.WindowDmgType != "crack01")
		{
			tileChangeManager.ChangeTile("crack01", cellPos, TileChangeLayer.WindowDamage);
			data.WindowDmgType = "crack01";
		}

		if (data.GetDamage >= 50 && data.GetDamage < 75 && data.WindowDmgType != "crack02")
		{
			tileChangeManager.ChangeTile("crack02", cellPos, TileChangeLayer.WindowDamage);
			data.WindowDmgType = "crack02";
		}

		if (data.GetDamage >= 75 && data.GetDamage < 100 && data.WindowDmgType != "crack03")
		{
			tileChangeManager.ChangeTile("crack03", cellPos, TileChangeLayer.WindowDamage);
			data.WindowDmgType = "crack03";
		}

		if (data.GetDamage >= 100 && data.WindowDmgType != "broken")
		{
			tileChangeManager.RemoveTile(cellPos, TileChangeLayer.Window);

			//Spawn 3 glass shards with different sprites:
			PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(bulletHitTarget),
				Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().SetSpriteAndScatter(0);

			PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(bulletHitTarget),
				Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().SetSpriteAndScatter(1);

			PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(bulletHitTarget),
				Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().SetSpriteAndScatter(2);

			//Play the breaking window sfx:
			PlaySoundMessage.SendToAll("GlassBreak0" + Random.Range(1, 4).ToString(), bulletHitTarget, 1f);

			data.WindowDmgType = "broken";
			data.ResetDamage();
		}
	}

	private void AddGrillDamage(int damage, MetaDataNode data, Vector3Int cellPos, Vector3 bulletHitTarget)
	{
		data.AddDamage(damage);

		//Make grills a little bit weaker (set to 60 hp):
		if (data.GetDamage >= 60)
		{
			tileChangeManager.RemoveTile(cellPos, TileChangeLayer.Object);
			tileChangeManager.ChangeTile("GrillDestroyed", cellPos, TileChangeLayer.BrokenGrill);

			PlaySoundMessage.SendToAll("GrillHit", bulletHitTarget, 1f);
			//Spawn rods:
			var rods = PoolManager.Instance.PoolNetworkInstantiate(rodsPrefab, Vector3Int.RoundToInt(bulletHitTarget), 
			Quaternion.identity,tileChangeManager.ObjectParent.transform);

			var netTransform = rods.GetComponent<CustomNetTransform>();
			netTransform?.SetPosition(netTransform.ServerState.WorldPosition + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)));

			data.ResetDamage();
		}
	}
}