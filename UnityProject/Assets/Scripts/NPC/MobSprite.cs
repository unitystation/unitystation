using System.Collections;
using Systems.Explosions;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using Core.Directionals;
using Effects.Overlays;
using Logs;

namespace Systems.Mob
{
	/// <summary>
	/// Easy to use Directional sprite handler for mobs
	/// </summary>
	public class MobSprite : NetworkBehaviour, IOnLightningHit
	{
		private enum MobStateRepType
		{
			None = 0,
			UseSprite = 1,
			UseRotation = 2
		}

		[Header("References")]
		[SerializeField]
		private SpriteHandler spriteHandler = default;
		[SerializeField]
		private SpriteRenderer spriteRend = default;

		[Header("Settings")]
		[SerializeField, BoxGroup("Sprites")]
		private int aliveSpriteIndex = 0;

		[Tooltip("How to represent a dead sprite: via sprite, rotation or do nothing.")]
		[SerializeField, BoxGroup("Sprites")]
		private MobStateRepType deadStateRep = MobStateRepType.UseSprite;
		private bool UsesDeadSprite => deadStateRep == MobStateRepType.UseSprite;
		private bool UsesDeadRotation => deadStateRep == MobStateRepType.UseRotation;
		[SerializeField, BoxGroup("Sprites"), ShowIf(nameof(UsesDeadSprite))]
		private int deadSpriteIndex = 1;
		[Tooltip("0 upright, 90 is prone with head to the left.")]
		[SerializeField, BoxGroup("Sprites"), ShowIf(nameof(UsesDeadRotation))]
		private float deadOrientation = 90;

		[SerializeField, BoxGroup("Sprites")]
		private MobStateRepType knockedDownStateRep = MobStateRepType.UseSprite;
		private bool UsesKnockedDownSprite => knockedDownStateRep == MobStateRepType.UseSprite;
		private bool UsesKnockedDownRotation => knockedDownStateRep == MobStateRepType.UseRotation;
		[SerializeField, BoxGroup("Sprites"), ShowIf(nameof(UsesKnockedDownSprite))]
		private int knockedDownSpriteIndex = 1;
		[Tooltip("0 upright, 90 is prone with head to the left.")]
		[SerializeField, BoxGroup("Sprites"), ShowIf(nameof(UsesKnockedDownRotation))]
		private float knockedDownOrientation = 90;

		[Tooltip("Assign the prefab responsible for the burning overlay.")]
		[SerializeField]
		private GameObject burningPrefab = default;

		[Tooltip("Assign the prefab responsible for the electrocuted overlay.")]
		[SerializeField]
		private GameObject electrocutedPrefab = default;

		private LivingHealthBehaviour health;
		private Rotatable rotatable;


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
			rotatable = GetComponent<Rotatable>();

			if (rotatable != null)
			{
				rotatable.OnRotationChange.AddListener(OnDirectionChange);
			}
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

		public void SetSprite(int index, bool network = false)
		{
			if (spriteHandler == null)
			{
				Loggy.LogWarning($"{nameof(SpriteHandler)} missing on {gameObject}!", Category.Mobs);
				return;
			}

			spriteHandler.ChangeSprite(index, network);
		}

		/// <summary>
		/// Set the mob sprite to the first alive sprite.
		/// </summary>
		public void SetToAlive(bool network = false)
		{
			switch (deadStateRep)
			{
				case MobStateRepType.UseSprite:
					SetSprite(aliveSpriteIndex, network);
					break;
				case MobStateRepType.UseRotation:
					SetRotationServer(0);
					break;
			}
		}

		/// <summary>
		/// Set the mob sprite to the dead sprite.
		/// </summary>
		public void SetToDead(bool network = false)
		{
			switch (deadStateRep)
			{
				case MobStateRepType.UseSprite:
					SetSprite(deadSpriteIndex, network);
					break;
				case MobStateRepType.UseRotation:
					SetRotationServer(deadOrientation);
					break;
			}
		}

		public void SetToKnockedDown(bool network = false)
		{
			switch (knockedDownStateRep)
			{
				case MobStateRepType.UseSprite:
					SetSprite(knockedDownSpriteIndex, network);
					break;
				case MobStateRepType.UseRotation:
					SetRotationServer(knockedDownOrientation);
					break;
			}
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

			burningOverlay.StartOverlay(rotatable.CurrentDirection);
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

			electrocutedOverlay.StartOverlay(rotatable.CurrentDirection);
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

		private void OnDirectionChange(OrientationEnum newDirection)
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

		public void OnLightningHit(float duration, float damage)
		{
			EnableElectrocutedOverlay(duration);
		}
	}
}
