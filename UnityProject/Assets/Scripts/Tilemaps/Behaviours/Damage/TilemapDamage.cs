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

	private GameObject glassShardPrefab;

	void Awake()
	{
		tileChangeManager = transform.root.GetComponent<TileChangeManager>();
		tileTrigger = transform.root.GetComponent<TileTrigger>();
		metaDataLayer = transform.parent.GetComponent<MetaDataLayer>();
		metaTileMap = transform.parent.GetComponent<MetaTileMap>();
		thisLayer = GetComponent<Layer>();
	}

	void Start(){
		glassShardPrefab = Resources.Load("GlassShard") as GameObject;
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
		if (thisLayer.LayerType == LayerType.Windows)
		{
			forceDir.z = 0;
			var bulletHitTarget = bullet.transform.position + (forceDir * 0.5f);
			var cellPos = tileChangeManager.windowTileMap.WorldToCell(bulletHitTarget);
			var data = metaDataLayer.Get(cellPos);
			var getTile = tileChangeManager.windowTileMap.GetTile(cellPos);
			if (getTile != null)
			{
				data.AddDamage(20);
				if(data.GetDamage >= 20 && data.GetDamage < 50 && data.WindowDmgType != "crack01"){
					tileChangeManager.ChangeTile("crack01", cellPos, TileChangeLayer.WindowDamage);
					data.WindowDmgType = "crack01";
				}

				if(data.GetDamage >= 50 && data.GetDamage < 75 && data.WindowDmgType != "crack02"){
					tileChangeManager.ChangeTile("crack02", cellPos, TileChangeLayer.WindowDamage);
					data.WindowDmgType = "crack02";
				}

				if(data.GetDamage >= 75 && data.GetDamage < 100 && data.WindowDmgType != "crack03"){
					tileChangeManager.ChangeTile("crack03", cellPos, TileChangeLayer.WindowDamage);
					data.WindowDmgType = "crack03";
				}

				if(data.GetDamage >= 100 && data.WindowDmgType != "broken"){
					tileChangeManager.RemoveTile(cellPos, TileChangeLayer.Window);

					//Spawn 3 glass shards with different sprites:
					PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(bulletHitTarget), 
					Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().spriteIndex = 0;

					PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(bulletHitTarget), 
					Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().spriteIndex = 1;

					PoolManager.Instance.PoolNetworkInstantiate(glassShardPrefab, Vector3Int.RoundToInt(bulletHitTarget), 
					Quaternion.identity, tileChangeManager.ObjectParent.transform).GetComponent<GlassShard>().spriteIndex = 2;
					
					//Play the breaking window sfx:
					PlaySoundMessage.SendToAll("GlassBreak0" + Random.Range(1,4).ToString(), bulletHitTarget, 1f);

					data.WindowDmgType = "broken";
					data.ResetDamage();
				}
				
			}
		}
	}
}