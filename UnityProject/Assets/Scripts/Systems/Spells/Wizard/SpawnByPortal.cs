using System;
using System.Collections;
using Logs;
using UnityEngine;
using Messages.Server;

namespace Systems.Spells.Wizard
{
	/// <summary>
	/// Spawns an object via a portal, animating the process. Informs clients about the animation.
	/// </summary>
	public class SpawnByPortal
	{
		public event Action<GameObject> OnPortalReady;
		public event Action<GameObject> OnObjectSpawned;
		public event Action<GameObject> OnObjectLanded;

		/// <summary>
		/// <inheritdoc cref="SpawnByPortal"/>
		/// </summary>
		public SpawnByPortal(GameObject entityPrefab, GameObject portalPrefab, Vector3 worldPosition)
		{
			ServerSpawnPortalAndObject(entityPrefab, portalPrefab, worldPosition, PortalSpawnInfo.DefaultSettings());
		}

		/// <summary>
		/// <inheritdoc cref="SpawnByPortal"/>
		/// </summary>
		/// <param name="entityPrefab">the entity which should be dropped from the portal</param>
		/// <param name="portalPrefab">the portal the entity drops out of</param>
		/// <param name="worldPosition">the position the entity will land at</param>
		/// <param name="settings">settings such as portal height, durations</param>
		public SpawnByPortal(GameObject entityPrefab, GameObject portalPrefab, Vector3 worldPosition, PortalSpawnInfo settings)
		{
			ServerSpawnPortalAndObject(entityPrefab, portalPrefab, worldPosition, settings);
		}

		/// <summary>
		/// Animates the portal. Intended to be called from PortalSpawnMessages.
		/// </summary>
		public static void AnimatePortal(GameObject portal, PortalSpawnInfo settings)
		{
			// Portal expands
			LeanTween.scale(portal, Vector3.one, settings.PortalOpenTime).setFrom(Vector3.zero);

			// Portal shrinks
			LeanTween.scale(portal, Vector3.zero, settings.PortalCloseTime).setDelay(settings.PortalOpenTime + settings.PortalSuspenseTime);
		}

		/// <summary>
		/// Animates the object. Intended to be called from PortalSpawnMessages.
		/// </summary>
		public static void AnimateObject(GameObject entity, PortalSpawnInfo settings)
		{
			Transform spriteObject = entity.transform.Find("Sprite");
			if (spriteObject == null)
			{
				spriteObject = entity.transform.Find("sprite");
			}
			if (spriteObject == null)
			{
				Loggy.LogError($"No Sprite object found on {entity}! Cannot animate with {nameof(SpawnByPortal)}.", Category.Spells);
			}
			float fallingTime = GetFallingTime(settings.PortalHeight);

			// Animate entity falling.
			LeanTween.moveLocalY(spriteObject.gameObject, 0, fallingTime).setFrom(settings.PortalHeight).setEaseInQuad();

			// Animate entity rotating during fall.
			if (settings.EntityRotate)
			{
				spriteObject.LeanRotateZ(UnityEngine.Random.Range(0, 720), fallingTime).setFrom(RandomUtils.RandomRotation2D().eulerAngles);
			}
		}

		private void ServerSpawnPortalAndObject(GameObject entityPrefab, GameObject portalPrefab, Vector3 worldPosition, PortalSpawnInfo settings)
		{
			GameObject portal = ServerSpawnPortal(portalPrefab, worldPosition, settings);
			PortalSpawnAnimateMessage.SendToVisible(portal, settings, PortalSpawnAnimateMessage.AnimateType.Portal);

			OnPortalReady += (_) =>
			{
				GameObject entity = ServerSpawnObject(entityPrefab, worldPosition, settings);
				PortalSpawnAnimateMessage.SendToVisible(entity, settings, PortalSpawnAnimateMessage.AnimateType.Entity);
			};
		}

		private GameObject ServerSpawnPortal(GameObject portalPrefab, Vector3 worldPosition, PortalSpawnInfo settings)
		{
			// Spawn portal at some "height" above the landing zone.
			worldPosition.y += settings.PortalHeight;
			GameObject portal = Spawn.ServerPrefab(portalPrefab, worldPosition).GameObject;
			UpdateManager.Instance.StartCoroutine(ServerRunPortalSequence(portal, settings));

			return portal;
		}

		private GameObject ServerSpawnObject(GameObject entityPrefab, Vector3 worldPosition, PortalSpawnInfo settings)
		{
			// Spawn object at landing zone, (sprite will move back up to worldPosition for animation).
			GameObject entity = Spawn.ServerPrefab(entityPrefab, worldPosition).GameObject;
			OnObjectSpawned?.Invoke(entity);
			UpdateManager.Instance.StartCoroutine(ServerRunObjectSequence(entity, settings));

			return entity;
		}

		private IEnumerator ServerRunPortalSequence(GameObject portal, PortalSpawnInfo settings)
		{
			yield return WaitFor.Seconds(settings.PortalOpenTime + (settings.PortalSuspenseTime / 2));
			OnPortalReady?.Invoke(portal);
			yield return WaitFor.Seconds(settings.PortalCloseTime + (settings.PortalSuspenseTime / 2));
			_ = Despawn.ServerSingle(portal);
		}

		private IEnumerator ServerRunObjectSequence(GameObject entity, PortalSpawnInfo settings)
		{
			yield return WaitFor.Seconds(GetFallingTime(settings.PortalHeight));
			OnObjectLanded?.Invoke(entity);
		}

		private static float GetFallingTime(float height)
		{
			return Mathf.Sqrt((height * 2) / 9.81f);
		}
	}

	/// <summary>
	/// Settings used to define portal and object spawn animation behaviour.
	/// </summary>
	public struct PortalSpawnInfo
	{
		public int PortalHeight;
		public float PortalOpenTime;
		public float PortalCloseTime;
		public float PortalSuspenseTime;

		public bool EntityRotate;

		public static PortalSpawnInfo DefaultSettings()
		{
			return new PortalSpawnInfo
			{
				PortalHeight = 2,
				PortalOpenTime = 0.5f,
				PortalCloseTime = 0.5f,
				PortalSuspenseTime = 0.5f,
				EntityRotate = true,
			};
		}
	}
}
