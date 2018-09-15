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
		DetermineAction(coll.gameObject);
	}

	private void DetermineAction(GameObject objectColliding)
	{
		var bulletBehaviour = objectColliding.GetComponent<BulletBehaviour>();
		if (bulletBehaviour != null)
		{
			DoBulletDamage(bulletBehaviour);
			return;
		}
	}

	private void DoBulletDamage(BulletBehaviour bullet)
	{
		if (thisLayer.LayerType == LayerType.Windows)
		{
			var cellPos = tileChangeManager.wallTileMap.WorldToCell((Vector2) bullet.transform.position + bullet.Direction);
			var data = metaDataLayer.Get(cellPos);
			data.AddDamage(40);
			Debug.Log("DAMAGE!: " + data.GetDamage);
		}
	}
}