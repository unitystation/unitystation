using Logs;
using UnityEngine;
using US13.Core.Lifecycle;
using US13.HealthV2.Living;
using US13.Managers.MatrixManager;
using US13.Managers.NetworkManagement;
using US13.Messages.Server;
using US13.Player;

namespace US13.Projectiles.Behaviours
{
	/// <summary>
	/// Identical to ProjectileDecal.cs, but creates a decal upon despawning instead of on hitting something.
	/// </summary>
	public class ProjectileDecalOnDespawn : MonoBehaviour, IOnDespawn
	{
		[SerializeField] private GameObject decal = null;

		[Tooltip("Living time of decal.")]
		[SerializeField] private float animationTime = 0;

		[Tooltip("Spawn decal on collision?")]
		[SerializeField] private bool isTriggeredOnHit = true;

		[Tooltip("Use PlayEffect")]
		[SerializeField] private bool UsePlayEffect = false;

		[Tooltip("Only trigger on player")]
		[SerializeField] private bool OnlyTriggerOnPlayer = false;

		[Tooltip("name for PlayEffect string")]
		[SerializeField] private GameObject PlayEffectObject;

		public void OnDespawn(MatrixManager.CustomPhysicsHit hit, Vector2 point)
		{
			if (isTriggeredOnHit && hit.ItHit)
			{
				OnBeamEnd(hit.HitWorld, hit.CollisionHit.GameObject);
			}
			else
			{
				OnBeamEnd(point, null);
			}
		}

		private void OnBeamEnd(Vector2 position, GameObject Collider)
		{
			if (UsePlayEffect == false)
			{
				var newDecal = Spawn.ClientPrefab(decal.name,
					position).GameObject;
				var timeLimitedDecal = newDecal.GetComponent<TimeLimitedDecal>();
				timeLimitedDecal.SetUpDecal(animationTime);
			}
			else
			{


				if (CustomNetworkManager.IsServer == false) return;
				if (Collider != null)
				{

					if (OnlyTriggerOnPlayer)
					{
						if (Collider.GetComponent<LivingHealthMasterBase>() == null) return;
					}
					float radians = (transform.localRotation.eulerAngles.z + 90) * Mathf.Deg2Rad;
					Vector3 localDirection = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * 0.75f;
					//Loggy.Error((Collider.transform.localPosition + localDirection).ToString());

					PlayEffect.SendToAll(Collider, PlayEffectObject.name,
						false,
						Collider,
						Collider.transform.localPosition + localDirection,
						transform.localRotation.eulerAngles.z + 45);
				}
			}
		}
	}
}