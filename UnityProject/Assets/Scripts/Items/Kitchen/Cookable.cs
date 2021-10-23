using System;
using UnityEngine;
using NaughtyAttributes;


namespace Items.Food
{
	/// <summary>
	/// This class doesn't handle cooking itself; it merely stores the cooking times
	/// and products for other objects (e.g. the microwave) to use when cooking.
	/// <para>The <see cref="OnCooked"/> event is raised when something cooks this, which other
	/// components can subscribe to, to perform extra logic (for e.g. microwaving dice to rig them).</para>
	/// </summary>
	public class Cookable : MonoBehaviour
	{
		[Tooltip("Minimum time to cook.")]
		public int CookTime = 10;

		[InfoBox("If no transormation is to take place, then don't select any item. Don't select the same item as itself.", EInfoBoxType.Warning)]
		[Tooltip("What this item becomes when cooked." +
				"If not set, this item will not change GameObject when cooked, but will still invoke the cooked event.")]
		public GameObject CookedProduct;

		[Tooltip("What methods this item can be cooked by.")]
		[EnumFlag]
		public CookSource CookableBy = CookSource.All;

		/// <summary>
		/// Raised when enough cooking time has been added (via <see cref="AddCookingTime(float)"/>)
		/// </summary>
		public event Action OnCooked;

		private float timeSpentCooking;

		private void Awake()
		{
			if (CookableBy.HasFlag(CookSource.BurnUp))
			{
				GetComponent<Integrity>().OnBurnUpServer += OnBurnUpServer;
			}
		}

		/// <summary>
		/// Adds the given cooking time to this object. Will return true if the item is now cooked.
		/// </summary>
		/// <param name="time">The amount of time in seconds to add to this object's time spent cooking.</param>
		/// <returns>true if the added time and any previous time spent cooking was enough to exceed the required cooking time.</returns>
		public bool AddCookingTime(float time)
		{
			timeSpentCooking += time;
			if (timeSpentCooking > CookTime)
			{
				OnCooked?.Invoke();
				return true;
			}

			return false;
		}

		private void OnBurnUpServer(DestructionInfo info)
		{
			_ = Despawn.ServerSingle(gameObject);
			Spawn.ServerPrefab(CookedProduct, gameObject.RegisterTile().WorldPosition, transform.parent);
		}
	}

	[Flags]
	public enum CookSource
	{
		BurnUp = 0,
		Microwave = 1 << 1,
		Griddle = 1 << 2,
		Oven = 1 << 3,
		All = ~0,
	}
}
