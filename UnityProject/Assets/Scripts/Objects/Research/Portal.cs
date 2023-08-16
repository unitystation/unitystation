using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Effects;
using Gateway;
using Items;
using Light2D;
using Systems.Explosions;
using UnityEngine;
using Weapons.Projectiles;
using Weapons.Projectiles.Behaviours;

namespace Objects.Research
{
	public class Portal : EnterTileBase, IOnPreHitDetect
	{
		private Portal connectedPortal;
		public Portal ConnectedPortal => connectedPortal;

		private static HashSet<Portal> portalPairs = new HashSet<Portal>();
		public static HashSet<Portal> PortalPairs => portalPairs;

		public UniversalObjectPhysics ObjectPhysics => objectPhysics;

		private SpriteHandler spriteHandler;
		private LightSprite lightSprite;

		private bool isOnCooldown => Time.time - lastActivationTime <= cooldownTime;
		private float lastActivationTime = 0.0f;

		[SerializeField] private float cooldownTime = 0.65f;

		protected override void Awake()
		{
			base.Awake();

			lastActivationTime = Time.time;
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			lightSprite = GetComponentInChildren<LightSprite>();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, PortalDeath);
			portalPairs.Remove(this);
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void StaticClear()
		{
			portalPairs.Clear();
		}

		public void SetBlue()
		{
			spriteHandler.ChangeSprite(0);
			ColorUtility.TryParseHtmlString("#0099FF", out var color);
			lightSprite.Color = color;
		}

		public void SetOrange()
		{
			spriteHandler.ChangeSprite(1);
			ColorUtility.TryParseHtmlString("#CC3300", out var color);
			lightSprite.Color = color;
		}

		public void PortalDeath()
		{
			if(CustomNetworkManager.IsServer == false) return;

			Chat.AddActionMsgToChat(gameObject, "The portal fizzles out into nothing!");

			//Despawn after time is up
			portalPairs.Remove(this);
			_ = Despawn.ServerSingle(gameObject);
		}

		public void SetNewPortal(Portal connectedPortal, int time = 300)
		{
			this.connectedPortal = connectedPortal;

			UpdateManager.Add(PortalDeath, time, false);

			portalPairs.Add(this);
		}

		public override bool WillAffectPlayer(PlayerScript playerScript)
		{
			//Allow players or ghosts to enter
			return playerScript.PlayerTypeSettings.CanEnterPortals;
		}

		public override void OnPlayerStep(PlayerScript playerScript)
		{
			if (isOnCooldown) return;
			_ = Teleport(playerScript.gameObject);
		}

		public override bool WillAffectObject(GameObject eventData)
		{
			//Don't allow intangible stuff, like sparks as that will cause loop crashes
			if (eventData.TryGetComponent<UniversalObjectPhysics>(out var uop) && uop.Intangible)
			{
				return false;
			}

			//Don't teleport tracking beacons as we open on them (might not be an issue but just incase)
			return eventData.GetComponent<TrackingBeacon>() == null;
		}

		public override void OnObjectEnter(GameObject eventData)
		{
			if (isOnCooldown) return;
			_ = Teleport(eventData);
		}

		private async Task Teleport(GameObject eventData)
		{
			if (connectedPortal == null || isOnCooldown) return;

			if (eventData.HasComponent<SparkEffect>()) return;
			if(eventData.TryGetComponent<UniversalObjectPhysics>(out var uop) == false) return;

			lastActivationTime = Time.time;
			connectedPortal.lastActivationTime = Time.time;

			TransportUtility.TransportObject(uop, connectedPortal.ObjectPhysics.OfficialPosition, false);
		}


		public bool OnPreHitDetect(OnHitDetectData data)
		{
			if(connectedPortal == null) return true;

			var range = -1f;

			if (data.BulletObject.TryGetComponent<ProjectileRangeLimited>(out var rangeLimited))
			{
				range = rangeLimited.CurrentDistance;
			}

			ProjectileManager.InstantiateAndShoot(data.BulletObject.GetComponent<Bullet>().PrefabName,
				data.BulletShootDirection, connectedPortal.gameObject, null, BodyPartType.None, range);

			Chat.AddActionMsgToChat(gameObject, $"The {data.BulletName} enters through the portal!");

			SparkUtil.TrySpark(gameObject, expose: false);
			SparkUtil.TrySpark(connectedPortal.gameObject, expose: false);

			return false;
		}
	}
}