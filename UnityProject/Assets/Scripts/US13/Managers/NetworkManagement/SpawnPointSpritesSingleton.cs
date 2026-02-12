using UnityEngine;
using US13.ScriptableObjects;

namespace US13.Managers.NetworkManagement
{
	[CreateAssetMenu(fileName = "SpawnPointSpritesSingleton", menuName = "Singleton/SpawnPointSpritesSingleton")]
	public class SpawnPointSpritesSingleton : SingletonScriptableObject<SpawnPointSpritesSingleton>
	{
		public SerializableDictionary<SpawnPointCategory, SpriteDataSO> Sprites = new SerializableDictionary<SpawnPointCategory, SpriteDataSO>();
	}
}
