using System.Collections;
using UnityEngine;
using Mirror;
using Core.Directionals;
using Effects.Overlays;

namespace Systems.Mob
{
	/// <summary>
	/// Easy to use Directional sprite handler for mobs
	/// </summary>
	[RequireComponent(typeof(DirectionalSpriteV2))]
	public class MobSprite : NetworkBehaviour
	{
		[Header("References")]
		[SerializeField]
		private SpriteHandler spriteHandler = default;
		[SerializeField]
		private SpriteRenderer spriteRend = default;

		[Header("Settings")]
		[SerializeField]
		private int aliveSpriteIndex = 0;
		[SerializeField]
		private int deadSpriteIndex = 1;

		[Tooltip("Assign the prefab responsible for the burning overlay.")]
		[SerializeField]
		private GameObject burningPrefab = default;

		[Tooltip("Assign the prefab responsible for the electrocuted overlay.")]
		[SerializeField]
		private GameObject electrocutedPrefab = default;

		private LivingHealthBehaviour health;
		private Directional directional;
		private DirectionalSpriteV2 directionalSprite;

		private PlayerDirectionalOverlay burningOverlay;
		private PlayerDirectionalOverlay electrocutedOverlay;

		[SyncVar(hook = nameof(SyncRotChange))] private float spriteRot;

		#region Lifecycle

		private void OnEnable()
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (health != null) return;
			health = GetComponent<LivingHealthBehaviour>();
			directional = GetComponent<Directional>();
			directionalSprite = GetComponent<DirectionalSpriteV2>();

			directional.OnDirectionChange.AddListener(OnDirectionChange);
		}

		public override void OnStartServer()
		{
			EnsureInit();
			spriteRot = 0;
		}

		public override void OnStartClient()
		{
			AddOverlayGameObjects();
			EnsureInit();
			SyncRotChange(spriteRot, spriteRot);
		}

		/// <summary>
		/// Instantiate and attach the sprite overlays if they don't exist
		/// </summary>
		private void AddOverlayGameObjects()
		{
			if (burningOverlay == null)
			{
				burningOverlay = Instantiate(burningPrefab, transform).GetComponent<PlayerDirectionalOverlay>();
				burningOverlay.enabled = true;
				burningOverlay.StopOverlay();
			}
			if (electrocutedOverlay == null)
			{
				electrocutedOverlay = Instantiate(electrocutedPrefab, transform).GetComponent<PlayerDirectionalOverlay>();
				electrocutedOverlay.enabled = true;
				electrocutedOverlay.StopOverlay();
			}
		}

		#endregion Lifecycle

		//The local rotation of the sprite obj
		private void SyncRotChange(float oldRot, float newRot)
		{
			EnsureInit();
			spriteRot = newRot;
			spriteRend.transform.localEulerAngles = new Vector3(0f, 0f, spriteRot);
		}

		/// <summary>
		/// Sets the local rotation of the sprite obj
		/// </summary>
		/// <param name="newRot"></param>
		public void SetRotationServer(float newRot)
		{
			spriteRot = newRot;
		}

		/// <summary>
		/// Set the mob sprite to the first alive sprite.
		/// </summary>
		public void SetToAlive(bool network = false)
		{
			if (spriteHandler == null)
			{
				Logger.LogWarning($"{nameof(SpriteHandler)} missing on {gameObject}!");
				return;
			}

			spriteHandler.ChangeSprite(aliveSpriteIndex, network);
		}

		/// <summary>
		/// Set the mob sprite to the dead sprite.
		/// </summary>
		public void SetToDead(bool network = false)
		{
			if (spriteHandler == null)
			{
				Logger.LogWarning($"{nameof(SpriteHandler)} missing on {gameObject}!");
				return;
			}

			spriteHandler.ChangeSprite(deadSpriteIndex, network);
		}

		/// <summary>
		/// Set the sprite renderer to bodies when the mob has died
		/// </summary>
		public void SetToBodyLayer()
		{
			spriteRend.sortingLayerName = "Bodies";
		}

		/// <summary>
		/// Set the mobs sprite renderer to NPC layer
		/// </summary>
		public void SetToNPCLayer()
		{
			spriteRend.sortingLayerName = "NPCs";
		}

		public void ClearBurningOverlay()
		{
			if (burningOverlay == null)
			{
				return;
			}

			burningOverlay.StopOverlay();
		}

		public void SetBurningOverlay()
		{
			if (burningOverlay == null)
			{
				AddOverlayGameObjects();
			}

			burningOverlay.StartOverlay(directional.CurrentDirection);
		}

		/// <summary>
		/// Enable the electrocuted overlay for the player's mob.
		/// </summary>
		/// <param name="time">If provided and greater than zero, how long until the electrocuted overlay is disabled.</param>
		public void EnableElectrocutedOverlay(float time = -1)
		{
			if (electrocutedOverlay == null)
			{
				AddOverlayGameObjects();
			}

			if (time > 0)
			{
				StartCoroutine(StopElectrocutedOverlayAfter(time));
			}

			electrocutedOverlay.StartOverlay(directional.CurrentDirection);
		}

		/// <summary>
		/// Disables the electrocuted overlay for the mob.
		/// </summary>
		public void DisableElectrocutedOverlay()
		{
			if (electrocutedOverlay == null)
			{
				return;
			}

			electrocutedOverlay.StopOverlay();
		}

		private void OnDirectionChange(Orientation newDirection)
		{
			if (burningOverlay != null && burningOverlay.OverlayActive)
			{
				SetBurningOverlay();
			}
		}

		private IEnumerator StopElectrocutedOverlayAfter(float seconds)
		{
			yield return WaitFor.Seconds(seconds);
			DisableElectrocutedOverlay();
		}
	}
}
