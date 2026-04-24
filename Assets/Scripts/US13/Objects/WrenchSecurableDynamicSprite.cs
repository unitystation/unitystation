using UnityEngine;
using US13.Core.Lifecycle;
using US13.Core.Sprite_Handler;
using US13.Systems.Construction;

namespace US13.Objects
{
	[RequireComponent(typeof(WrenchSecurable))]
	public class WrenchSecurableDynamicSprite : MonoBehaviour, IServerLifecycle
	{
		[SerializeField, Tooltip("SpriteHandler to update when the object is secured/unsecured.")]
		private SpriteHandler spriteHandler = null;

		[SerializeField, Tooltip("Catalogue index to use when the object is unsecured (not wrenched).")]
		private int unsecuredCatalogueIndex = 0;

		[SerializeField, Tooltip("Catalogue index to use when the object is secured (wrenched down).")]
		private int securedCatalogueIndex = 1;

		private WrenchSecurable wrenchSecurable;

		private void Awake()
		{
			wrenchSecurable = GetComponent<WrenchSecurable>();
			if (spriteHandler == null)
			{
				spriteHandler = GetComponentInChildren<SpriteHandler>();
			}
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (wrenchSecurable == null)
			{
				wrenchSecurable = GetComponent<WrenchSecurable>();
			}

			if (wrenchSecurable != null)
			{
				wrenchSecurable.OnAnchoredChange.AddListener(OnAnchoredChanged);
			}

			// server sets initial sprite
			UpdateSprite();
		}

		public void OnDespawnServer(DespawnInfo info)
		{
			if (wrenchSecurable != null)
			{
				wrenchSecurable.OnAnchoredChange.RemoveListener(OnAnchoredChanged);
			}
		}

		private void OnAnchoredChanged()
		{
			UpdateSprite();
		}

		private void UpdateSprite()
		{
			if (spriteHandler == null || wrenchSecurable == null) return;

			int index = wrenchSecurable.IsAnchored ? securedCatalogueIndex : unsecuredCatalogueIndex;
			spriteHandler.SetCatalogueIndexSprite(index);
		}
	}
}