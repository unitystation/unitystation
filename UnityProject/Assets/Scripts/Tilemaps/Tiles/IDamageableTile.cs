using UnityEngine;

public interface IDamageableTile
{
	float AddDamage(float damage, AttackType attackType, Vector3Int cellPos, Vector3 worldPosition, MetaDataNode data,
		TileChangeManager tileChangeManager);
}