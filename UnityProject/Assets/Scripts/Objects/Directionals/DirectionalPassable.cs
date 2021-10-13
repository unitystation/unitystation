using System;
using UnityEngine;
using NaughtyAttributes;

namespace Core.Directionals
{
	/// <summary>
	/// Allows only some sides of an objects's tile to be passable. Useful for things such as directional windows, disposal intakes.
	/// Must be the sole RegisterTile-derived component on the object, else it is glitchy.
	/// </summary>
	[RequireComponent(typeof(Directional))]
	public class DirectionalPassable : RegisterObject
	{
		[InfoBox("Set Passable or Atmos Passable checked if any side is passable by default, or any atmos passable side", EInfoBoxType.Normal)]
		[Header("Overrides")]
		[SerializeField]
		private bool isLeavableOnAll = default;
		[SerializeField]
		private bool isEnterableOnAll = default;
		[SerializeField]
		private bool isAtmosPassableOnAll = default;

		[Header("Set which sides are passable when in orientation Down")]
		[SerializeField]
		private PassableSides leavableSides = default;
		[SerializeField]
		private PassableSides enterableSides = default;
		[SerializeField]
		private PassableSides atmosphericPassableSides = default;

		public Directional Directional { get; private set; }

		public bool IsLeavableOnAll => isLeavableOnAll;
		public bool IsEnterableOnAll => isEnterableOnAll;
		public bool IsAtmosPassableOnAll => isAtmosPassableOnAll;

		#region RegisterObject Overrides

		protected override void Awake()
		{
			base.Awake();
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (Directional != null) return;

			Directional = GetComponent<Directional>();
		}

		public override bool IsPassable(bool isServer, GameObject context = null)
		{
			if (context == gameObject) return true;

			return Passable;
		}

		public override bool IsPassableFromOutside(Vector3Int enteringFrom, bool isServer, GameObject context = null)
		{
			if (context == gameObject) return true;
			if (IsEnterableOnAll) return true;
			if (Passable == false) return false;

			return IsPassableAtSide(GetSideFromVector(enteringFrom), enterableSides);
		}

		public override bool IsPassableFromInside(Vector3Int leavingTo, bool isServer, GameObject context = null)
		{
			if (context == gameObject) return true;
			if (IsLeavableOnAll) return true;
			if (Passable == false) return true;

			return IsPassableAtSide(GetSideFromVector(leavingTo), leavableSides);
		}

		public override bool DoesNotBlockClick(Vector3Int reachingFrom, bool isServer)
		{
			if (IsLeavableOnAll) return true;
			if (Passable == false) return true;

			return IsPassableAtSide(GetSideFromVector(reachingFrom), leavableSides);
		}

		public override bool IsAtmosPassable(Vector3Int enteringFrom, bool isServer)
		{
			if (IsAtmosPassableOnAll) return true;
			if (AtmosPassable == false) return false;

			return IsPassableAtSide(GetSideFromVector(enteringFrom), atmosphericPassableSides);
		}

		#endregion RegisterObject Overrides

		/// <summary>
		/// Sets all sides for this object as passable for the given pass type.
		/// </summary>
		public void AllowPassableOnAllSides(PassType passType)
		{
			switch (passType)
			{
				case PassType.Entering:
					isEnterableOnAll = true;
					SetPassable(false, true);
					break;
				case PassType.Leaving:
					isLeavableOnAll = true;
					SetPassable(false, true);
					break;
				case PassType.Atmospheric:
					isAtmosPassableOnAll = true;
					AtmosPassable = true;
					break;
				default:
					Logger.LogWarning("Unknown DirectionalPassable PassType. Doing nothing.", Category.Directionals);
					break;
			}
		}

		/// <summary>
		/// Sets all sides for this object as impassable for the given pass type.
		/// </summary>
		public void DenyPassableOnAllSides(PassType passType)
		{
			switch (passType)
			{
				case PassType.Entering:
					isEnterableOnAll = false;
					SetPassable(false, false);
					break;
				case PassType.Leaving:
					isLeavableOnAll = false;
					SetPassable(false, false);
					break;
				case PassType.Atmospheric:
					isAtmosPassableOnAll = false;
					AtmosPassable = false;
					break;
				default:
					Logger.LogWarning("Unknown DirectionalPassable PassType. Doing nothing.", Category.Directionals);
					break;
			}
		}

		/// <summary>
		/// Sets only the sides defined as passable for the given pass type.
		/// </summary>
		public void AllowPassableAtSetSides(PassType passType)
		{
			switch (passType)
			{
				case PassType.Entering:
					isEnterableOnAll = false;
					SetPassable(false, true);
					break;
				case PassType.Leaving:
					isLeavableOnAll = false;
					SetPassable(false, true);
					break;
				case PassType.Atmospheric:
					isAtmosPassableOnAll = false;
					AtmosPassable = true;
					break;
				default:
					Logger.LogWarning("Unknown DirectionalPassable PassType. Doing nothing.", Category.Directionals);
					break;
			}
		}

		private Vector2Int GetSideFromVector(Vector3Int vector)
		{
			return (vector - LocalPosition).To2Int();
		}

		private bool IsPassableAtSide(Vector2Int sideToCross, PassableSides sides)
		{
			EnsureInit();
			if (Directional == null)
			{
				Logger.LogError($"No {nameof(Directional)} component found on {this}?", Category.Directionals);
				return false;
			}

			if (sideToCross == Vector2Int.zero) return true;

			// TODO: figure out a better way or at least use some data structure.
			switch (Directional.CurrentDirection.AsEnum())
			{
				case OrientationEnum.Up:
					if (sideToCross == Vector2Int.up) return sides.Down;
					else if (sideToCross == Vector2Int.down) return sides.Up;
					else if (sideToCross == Vector2Int.left) return sides.Right;
					else if (sideToCross == Vector2Int.right) return sides.Left;
					break;
				case OrientationEnum.Down:
					if (sideToCross == Vector2Int.up) return sides.Up;
					else if (sideToCross == Vector2Int.down) return sides.Down;
					else if (sideToCross == Vector2Int.left) return sides.Left;
					else if (sideToCross == Vector2Int.right) return sides.Right;
					break;
				case OrientationEnum.Left:
					if (sideToCross == Vector2Int.up) return sides.Left;
					else if (sideToCross == Vector2Int.down) return sides.Right;
					else if (sideToCross == Vector2Int.left) return sides.Down;
					else if (sideToCross == Vector2Int.right) return sides.Up;
					break;
				case OrientationEnum.Right:
					if (sideToCross == Vector2Int.up) return sides.Right;
					else if (sideToCross == Vector2Int.down) return sides.Left;
					else if (sideToCross == Vector2Int.left) return sides.Up;
					else if (sideToCross == Vector2Int.right) return sides.Down;
					break;
				default:
					Logger.LogWarning("Unknown orientation. Returning false.", Category.Directionals);
					break;
			}

			return false;
		}

		[Serializable]
		public struct PassableSides
		{
			public bool Up;
			public bool Down;
			public bool Left;
			public bool Right;
		}
	}

	public enum PassType
	{
		Entering,
		Leaving,
		Atmospheric,
	}
}
