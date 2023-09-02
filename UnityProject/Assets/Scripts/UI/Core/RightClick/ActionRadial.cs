using System.Collections.Generic;
using Logs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using UI.Core.Radial;
using Util;

namespace UI.Core.RightClick
{
	public class ActionRadial : Radial<RightClickRadialButton>
	{
		[SerializeField]
		private RectTransform borderPrefab = default;

		[SerializeField]
		private Image radialMask = default;

		[SerializeField]
		private Transform background = default;

		private RectTransform startBorder;

		private RectTransform endBorder;

		private ParentConstraint parentConstraint;

		private RectTransform BorderPrefab =>
			this.VerifyNonChildReference(borderPrefab, "a rectangular image prefab", category: Category.UI);

		private Image RadialMask => VerifyChildReference(ref radialMask, "a masked image", "BackgroundMask");

		private Transform Background => VerifyChildReference(ref background, "a background image", "RadialActionRing");

		private ParentConstraint ParentConstraint => this.GetComponentByRef(ref parentConstraint);

		private T InitOrGet<T>(ref T obj, T prefab) where T : Component
		{
			if (obj == null && prefab != null)
			{
				obj = Instantiate(prefab, transform);
				obj.SetActive(true);
			}

			return obj;
		}

		private RectTransform StartBorder => InitOrGet(ref startBorder, BorderPrefab);

		private RectTransform EndBorder => InitOrGet(ref endBorder, BorderPrefab);

		public void LateUpdate()
		{
			if (Background)
			{
				Background.rotation = Quaternion.identity;
			}
		}

		public override void Setup(int itemCount)
		{
			ArcMeasure = itemCount * (360 / MaxShownItems);
			base.Setup(itemCount);

			if (RadialMask)
			{
				RadialMask.fillAmount = (1f / 360f) * ArcMeasure;
			}

			if (StartBorder != null && EndBorder != null)
			{
				StartBorder.localPosition = new Vector3(0, -.5f, 0);
				EndBorder.localEulerAngles = new Vector3(0f, 0f, ItemArcMeasure * itemCount);
				EndBorder.localPosition = new Vector3(-0.5f, 0, 0);
			}

			if (Items.Count > 0)
			{
				Items[0].SetDividerActive(false);
			}
		}

		public void SetupWithActions(IList<RightClickMenuItem> actions)
		{
			Setup(actions.Count);
			Selected.OrNull()?.ResetState();

			for (var i = 0; i < actions.Count; i++)
			{
				if (i >= Items.Count)
				{
					Loggy.LogError("Too many subentries on Right click menu");
					continue;
				}
				Items[i].ChangeItem(actions[i]);
			}
			this.SetActive(true);
		}

		public void UpdateRotation(int index, float angle)
		{
			var buttonAngle = index * angle;
			var zOffset = buttonAngle + (angle * 0.5f - ShownItemsCount * 0.5f * ItemArcMeasure);
			ParentConstraint.SetRotationOffset(0, new Vector3(0, 0, zOffset));
		}

		public void SetConstraintSource(Transform rotationSource)
		{
			ParentConstraint.AddSource(new ConstraintSource {sourceTransform = rotationSource, weight = 1});
			ParentConstraint.constraintActive = true;
		}
	}
}
