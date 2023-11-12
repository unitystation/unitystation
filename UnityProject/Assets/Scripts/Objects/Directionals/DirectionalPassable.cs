using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEngine;
using NaughtyAttributes;
using Util;

namespace Core.Directionals
{
	/// <summary>
	/// Allows only some sides of an objects's tile to be passable. Useful for things such as directional windows, disposal intakes.
	/// Must be the sole RegisterTile-derived component on the object, else it is glitchy.
	/// </summary>
	[RequireComponent(typeof(Rotatable))]
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

		private CheckedComponent<Rotatable> rotatableChecked = new CheckedComponent<Rotatable>();
		public CheckedComponent<Rotatable> RotatableChecked => rotatableChecked;

		public bool IsLeavableOnAll => isLeavableOnAll;
		public bool IsEnterableOnAll => isEnterableOnAll;
		public bool IsAtmosPassableOnAll => isAtmosPassableOnAll;

		public PassableSides AtmosphericPassableSides => atmosphericPassableSides;

		#region RegisterObject Overrides

		protected override void Awake()
		{
			base.Awake();
			EnsureInit();
		}

		private void OnEnable()
		{
			if(ObjectPhysics.HasComponent == false) return;
			ObjectPhysics.Component.OnLocalTileReached.AddListener(OnLocalTileChange);
		}

		private void OnDisable()
		{
			if(ObjectPhysics.HasComponent == false) return;
			ObjectPhysics.Component.OnLocalTileReached.RemoveListener(OnLocalTileChange);
		}

		private void EnsureInit()
		{
			if (rotatableChecked.HasComponent) return;

			rotatableChecked.ResetComponent(this);
		}

		public override bool IsPassable(bool isServer, GameObject context = null)
		{
			if (context != null)
			{
				if (context == gameObject) return true;
			}


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
			//If despawning then always be atmos passable
			if (Active == false) return true;

			if (IsAtmosPassableOnAll) return true;
			if (AtmosPassable == false) return false;

			return IsPassableAtSide(GetSideFromVector(enteringFrom), atmosphericPassableSides);
		}

		#endregion RegisterObject Overrides

		private void OnLocalTileChange(Vector3Int oldLocalPos, Vector3Int newLocalPos)
		{
			//We have moved from old spot so redo atmos blocks for new and old positions
			UpdateSubsystemsAt(oldLocalPos);

			if(oldLocalPos == newLocalPos) return;

			UpdateSubsystemsAt(newLocalPos);
		}

		private void UpdateSubsystems()
		{
			if(ObjectPhysics.HasComponent == false) return;
			Matrix.TileChangeManager.SubsystemManager.UpdateAt(ObjectPhysics.Component.OfficialPosition.ToLocalInt(Matrix));
		}

		private void UpdateSubsystemsAt(Vector3Int localPos)
		{
			if(ObjectPhysics.HasComponent == false) return;
			Matrix.TileChangeManager.SubsystemManager.UpdateAt(ObjectPhysics.Component.OfficialPosition.ToLocalInt(Matrix));
		}

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
					Loggy.LogWarning("Unknown DirectionalPassable PassType. Doing nothing.", Category.Directionals);
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
					Loggy.LogWarning("Unknown DirectionalPassable PassType. Doing nothing.", Category.Directionals);
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
					Loggy.LogWarning("Unknown DirectionalPassable PassType. Doing nothing.", Category.Directionals);
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
			if (rotatableChecked.HasComponent == false)
			{
				Loggy.LogError($"No {nameof(Rotatable)} component found on {this}?", Category.Directionals);
				return false;
			}

			if (sideToCross == Vector2Int.zero) return true;

			// TODO: figure out a better way or at least use some data structure.
			switch (rotatableChecked.Component.CurrentDirection)
			{
				case OrientationEnum.Up_By0:
					if (sideToCross == Vector2Int.up) return sides.Down;
					else if (sideToCross == Vector2Int.down) return sides.Up;
					else if (sideToCross == Vector2Int.left) return sides.Right;
					else if (sideToCross == Vector2Int.right) return sides.Left;
					break;
				case OrientationEnum.Down_By180:
					if (sideToCross == Vector2Int.up) return sides.Up;
					else if (sideToCross == Vector2Int.down) return sides.Down;
					else if (sideToCross == Vector2Int.left) return sides.Left;
					else if (sideToCross == Vector2Int.right) return sides.Right;
					break;
				case OrientationEnum.Left_By90:
					if (sideToCross == Vector2Int.up) return sides.Left;
					else if (sideToCross == Vector2Int.down) return sides.Right;
					else if (sideToCross == Vector2Int.left) return sides.Down;
					else if (sideToCross == Vector2Int.right) return sides.Up;
					break;
				case OrientationEnum.Right_By270:
					if (sideToCross == Vector2Int.up) return sides.Right;
					else if (sideToCross == Vector2Int.down) return sides.Left;
					else if (sideToCross == Vector2Int.left) return sides.Up;
					else if (sideToCross == Vector2Int.right) return sides.Down;
					break;
				default:
					Loggy.LogWarning("Unknown orientation. Returning false.", Category.Directionals);
					break;
			}

			return false;
		}

		public IEnumerable<OrientationEnum> GetOrientationsBlocked(PassableSides sides)
		{
			EnsureInit();
			if (rotatableChecked.HasComponent == false)
			{
				Loggy.LogError($"No {nameof(Rotatable)} component found on {this}?", Category.Directionals);
				return Enumerable.Empty<OrientationEnum>();
			}

			var enums = new List<OrientationEnum>(4);

			// TODO: figure out a better way or at least use some data structure.
			switch (rotatableChecked.Component.CurrentDirection)
			{
				case OrientationEnum.Up_By0:
					if (sides.Down == false) enums.Add(OrientationEnum.Up_By0);
					if (sides.Up == false) enums.Add(OrientationEnum.Down_By180);
					if (sides.Right == false) enums.Add(OrientationEnum.Left_By90);
					if (sides.Left == false) enums.Add(OrientationEnum.Right_By270);
					break;
				case OrientationEnum.Down_By180:
					if (sides.Up == false) enums.Add(OrientationEnum.Up_By0);
					if (sides.Down == false) enums.Add(OrientationEnum.Down_By180);
					if (sides.Left == false) enums.Add(OrientationEnum.Left_By90);
					if (sides.Right == false) enums.Add(OrientationEnum.Right_By270);
					break;
				case OrientationEnum.Left_By90:
					if (sides.Left == false) enums.Add(OrientationEnum.Up_By0);
					if (sides.Right == false) enums.Add(OrientationEnum.Down_By180);
					if (sides.Down == false) enums.Add(OrientationEnum.Left_By90);
					if (sides.Up == false) enums.Add(OrientationEnum.Right_By270);
					break;
				case OrientationEnum.Right_By270:
					if (sides.Right == false) enums.Add(OrientationEnum.Up_By0);
					if (sides.Left == false) enums.Add(OrientationEnum.Down_By180);
					if (sides.Up == false) enums.Add(OrientationEnum.Left_By90);
					if (sides.Down == false) enums.Add(OrientationEnum.Right_By270);
					break;
				default:
					Loggy.LogWarning("Unknown orientation. Returning false.", Category.Directionals);
					break;
			}

			return enums;
		}

		public override void OnDespawnServer(DespawnInfo info)
		{
			base.OnDespawnServer(info);

			if (Matrix == null) return;

			Matrix.MatrixInfo.SubsystemManager.UpdateAt(LocalPositionServer);
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
