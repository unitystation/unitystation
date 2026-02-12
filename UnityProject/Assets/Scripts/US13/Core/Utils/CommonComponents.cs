using System;
using System.Collections.Generic;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Items;
using US13.Objects.Directionals;
using US13.Player;
using US13.Tilemaps.Behaviours.Objects;
using UniversalObjectPhysics = US13.Core.Physics.UniversalObjectPhysics;

namespace US13.Core.Utils
{
	public class CommonComponents : MonoBehaviour
	{

		public UniversalObjectPhysics UniversalObjectPhysics => SafeGetComponent<UniversalObjectPhysics>();

		public RegisterTile RegisterTile => SafeGetComponent<RegisterTile>();

		public PlayerScript PlayerScript => SafeGetComponent<PlayerScript>();

		public Rotatable Rotatable => SafeGetComponent<Rotatable>();

		public LivingHealthMasterBase LivingHealth => SafeGetComponent<LivingHealthMasterBase>();

		public ItemAttributesV2 ItemAttributes => SafeGetComponent<ItemAttributesV2>();

		public IPlayerPossessable IPlayerPossessable;

		public Dictionary<Type, Component> dictionary = new Dictionary<Type, Component>();

		[SerializeField] private List<Component> commonComponentsToRegister = new List<Component>();


		private void Awake()
		{
			IPlayerPossessable = this.GetComponent<IPlayerPossessable>();
			ComponentsTracker<CommonComponents>.RegisterInstance(this);
			foreach (var c in commonComponentsToRegister)
			{
				if (c == null) continue;
				var type = c.GetType();
				dictionary.TryAdd(type, c);
			}
		}

		private void OnDestroy()
		{
			ComponentsTracker<CommonComponents>.UnregisterInstance(this);
		}

		public bool TrySafeGetComponent<T>(out T component) where T : Component
		{
			if (dictionary.ContainsKey(typeof(T)) == false)
			{
				dictionary[typeof(T)] = this.GetComponent<T>();
			}

			component = dictionary[typeof(T)] as T;
			return component != null;
		}

		public T SafeGetComponent<T>() where T : Component
		{
			if (dictionary.ContainsKey(typeof(T)) == false)
			{
				dictionary[typeof(T)] = this.GetComponent<T>();
			}

			return dictionary[typeof(T)] as T;
		}

	}
}
