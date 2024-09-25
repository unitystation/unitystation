using System.Collections;
using System.Collections.Generic;
using ScriptableObjects;
using Systems.Spawns;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnPointSpritesSingleton", menuName = "Singleton/SpawnPointSpritesSingleton")]
public class SpawnPointSpritesSingleton : SingletonScriptableObject<SpawnPointSpritesSingleton>
{
	public SerializableDictionary<SpawnPointCategory, SpriteDataSO> Sprites = new SerializableDictionary<SpawnPointCategory, SpriteDataSO>();
}
