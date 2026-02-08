using Cysharp.Threading.Tasks;
using UnityEngine;
using US13.HealthV2.Living.CirculatorySystem;
using US13.Tilemaps.Behaviours.Layers;

namespace US13.Systems.Explosions.NodeTypes
{
	public class HarmlessExplosionNode : ExplosionNode
	{

		public HarmlessExplosionNode(Vector3 _explosionWorldStartPosition) : base(_explosionWorldStartPosition)
		{
			//No other constructor logic needed
		}
		public override UniTask Process()
		{
			//Harmless explosives can't affect the environment at all, so no checks and/or additional methods are required
			return UniTask.CompletedTask;
		}

		public override float DoDamageToTiles(Matrix matrix, float damageDealt, Vector3Int v3int, MetaTileMap tileMap)
		{
			return 0;
		}

		public override void DoInternalDamage(float strength, BodyPart bodyPart)
		{
			return; //todo: add damage to prosthetics and augs
		}

		public override ExplosionNode GenInstance()
		{
			return new HarmlessExplosionNode(ExplosionStartWorldPosition);
		}
	}
}
