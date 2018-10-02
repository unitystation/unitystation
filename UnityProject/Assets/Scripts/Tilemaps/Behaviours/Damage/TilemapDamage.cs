using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TilemapDamage : MonoBehaviour
{
	private TileChangeManager tileChangeManager;
	private TileTrigger tileTrigger;

	private MetaDataLayer metaDataLayer;
	private MetaTileMap metaTileMap;

	public Layer Layer { get; private set; }

	//FIXME: cache construction prefabs in CraftingManager.Construction:
	private GameObject glassShardPrefab;
	private GameObject rodsPrefab;

	void Awake()
	{
		tileChangeManager = transform.root.GetComponent<TileChangeManager>();
		tileTrigger = transform.root.GetComponent<TileTrigger>();
		metaDataLayer = transform.parent.GetComponent<MetaDataLayer>();
		metaTileMap = transform.parent.GetComponent<MetaTileMap>();
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
		var bulletHitTarget = bullet.transform.position + (forceDir * 0.2f);
		var cellPos = tileChangeManager.baseTileMap.WorldToCell(bulletHitTarget);
		var data = metaDataLayer.Get(cellPos);

		if (Layer.LayerType == LayerType.Windows)
		{
			var getTile = tileChangeManager.windowTileMap.GetTile(cellPos);
			if (getTile != null)
			{
				//TODO damage amt based off type of bullet
				AddWindowDamage(bullet.damage, data, cellPos, bulletHitTarget);
				return;
			}
		}
		if (Layer.LayerType == LayerType.Grills)
		{
			var getWindowTile = tileChangeManager.windowTileMap.GetTile(cellPos);

			//Make sure a window is not protecting it first:
			if (!getWindowTile)
			{
				var getGrillTile = tileChangeManager.grillTileMap.GetTile(cellPos);
				if (getGrillTile != null)
				{
					//TODO damage amt based off type of bullet
					AddGrillDamage(bullet.damage, data, cellPos, bulletHitTarget);
				}
			}
		}
	}

	public void DoThrowDamage(Vector3Int worldTargetPos, ThrowInfo throwInfo, int dmgAmt)
	{
		DoMeleeDamage(new Vector2(worldTargetPos.x,worldTargetPos.y), throwInfo.ThrownBy, dmgAmt );
	}
	//Only works serverside:
	public void DoMeleeDamage(Vector2 dmgPosition, GameObject originator, int dmgAmt)
	{
		var cellPos = tileChangeManager.baseTileMap.WorldToCell(dmgPosition);
		var data = metaDataLayer.Get(cellPos);

		if (Layer.LayerType == LayerType.Windows)
		{
			var getTile = tileChangeManager.windowTileMap.GetTile(cellPos);
			if (getTile != null)
			{
				PlaySoundMessage.SendToAll("GlassHit", dmgPosition, Random.Range(0.9f, 1.1f));
				AddWindowDamage(dmgAmt, data, cellPos, dmgPosition);
				return;
			}
		}

		if (Layer.LayerType == LayerType.Grills)
		{

			var getWindowTile = tileChangeManager.windowTileMap.GetTile(cellPos);

			//Make sure a window is not protecting it first:
			if (!getWindowTile)
			{
				var getGrillTile = tileChangeManager.grillTileMap.GetTile(cellPos);
				if (getGrillTile != null)
				{
					PlaySoundMessage.SendToAll("GrillHit", dmgPosition, Random.Range(0.9f, 1.1f));
					AddGrillDamage(dmgAmt, data, cellPos, dmgPosition);
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
			SpawnGlassShards(bulletHitTarget);

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
			tileChangeManager.RemoveTile(cellPos, TileChangeLayer.Grill);
			tileChangeManager.ChangeTile("GrillDestroyed", cellPos, TileChangeLayer.BrokenGrill);

			PlaySoundMessage.SendToAll("GrillHit", bulletHitTarget, 1f);

			//Spawn rods:
			SpawnRods(bulletHitTarget);

			data.ResetDamage();
		}
	}

	//Only works server side:
	public void WireCutGrill(Vector3 snipPosition)
	{
		var cellPos = tileChangeManager.baseTileMap.WorldToCell(snipPosition);
		var data = metaDataLayer.Get(cellPos);

		if (Layer.LayerType == LayerType.Grills)
		{

			var getWindowTile = tileChangeManager.windowTileMap.GetTile(cellPos);

			//Make sure a window is not protecting it first:
			if (!getWindowTile)
			{
				var getGrillTile = tileChangeManager.grillTileMap.GetTile(cellPos);
				if (getGrillTile != null)
				{
					tileChangeManager.RemoveTile(cellPos, TileChangeLayer.Grill);

					PlaySoundMessage.SendToAll("WireCutter", snipPosition, 1f);
					SpawnRods(snipPosition);
				}
			}
		}

		data.ResetDamage();
	}

	private void SpawnRods(Vector3 pos)
	{
		var rods = PoolManager.Instance.PoolNetworkInstantiate(rodsPrefab, Vector3Int.RoundToInt(pos),
			Quaternion.identity, tileChangeManager.ObjectParent.transform);

		var netTransform = rods.GetComponent<CustomNetTransform>();
		netTransform?.SetPosition(netTransform.ServerState.WorldPosition + new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f)));
	}

	private void SpawnGlassShards(Vector3 pos){

			//Spawn 3 glass shards with different sprites:
			PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(pos),
				Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().SetSpriteAndScatter(0);

			PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(pos),
				Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().SetSpriteAndScatter(1);

			PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(pos),
				Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().SetSpriteAndScatter(2);

			//Play the breaking window sfx:
			PlaySoundMessage.SendToAll("GlassBreak0" + Random.Range(1, 4).ToString(), pos, 1f);
	}
}