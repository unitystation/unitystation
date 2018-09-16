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

	void Awake()
	{
		tileChangeManager = transform.root.GetComponent<TileChangeManager>();
		tileTrigger = transform.root.GetComponent<TileTrigger>();
		metaDataLayer = transform.parent.GetComponent<MetaDataLayer>();
		metaTileMap = transform.parent.GetComponent<MetaTileMap>();
		thisLayer = GetComponent<Layer>();
	}
	public void OnCollisionEnter2D(Collision2D coll)
	{
		var firstContact = coll.GetContact(0);
		var dirOfForce = (firstContact.point - (Vector2)coll.transform.position).normalized;
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

	private void DoBulletDamage(BulletBehaviour bullet, Vector2 forceDir)
	{
		if (thisLayer.LayerType == LayerType.Windows)
		{
			var cellPos = tileChangeManager.windowTileMap.WorldToCell((Vector2) bullet.transform.position + (forceDir * 0.5f));
			var data = metaDataLayer.Get(cellPos);
			var getTile = tileChangeManager.windowTileMap.GetTile(cellPos);
			if (getTile != null)
			{
				data.AddDamage(40);
				Debug.Log("OBJ NAME: " + getTile.ToString());
				Debug.Log("DAMAGE!: " + data.GetDamage);
			}
		}
	}
}