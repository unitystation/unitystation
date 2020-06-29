using UnityEngine;

public interface IDamageableTile
{
	float AddDamage(float damage, MetaDataNode data, Vector3Int cellPos, AttackType attackType, TileChangeManager tileChangeManager);
}