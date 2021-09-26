using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UI;
using UI.Core.Radial;

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

		private ParentConstraint ParentConstraint => this.GetComponentByRef(ref parentConstraint);

		private T InitOrGet<T>(ref T obj, T prefab) where T : Component
		{
			if (obj == null)
			{
				obj = Instantiate(prefab, transform);
				obj.SetActive(true);
			}

			return obj;
		}

		private RectTransform StartBorder => InitOrGet(ref startBorder, borderPrefab);

		private RectTransform EndBorder => InitOrGet(ref endBorder, borderPrefab);

		public void LateUpdate()
		{
			try
			{
				background.rotation = Quaternion.identity;
			}
			catch (NullReferenceException exception)
			{
				Logger.LogError("Caught a NRE in ItemRadial.LateUpdate() " + exception.Message, Category.UI);
			}
			catch (UnassignedReferenceException exception)
			{
				Logger.LogError("Caught an Unassigned Reference Exception in ItemRadial.LateUpdate() " + exception.Message, Category.UI);
			}
		}

		public override void Setup(int itemCount)
		{
			ArcMeasure = itemCount * (360 / MaxShownItems);
			base.Setup(itemCount);
			radialMask.fillAmount = (1f / 360f) * ArcMeasure;
			StartBorder.localPosition = new Vector3(0, -.5f, 0);
			EndBorder.localEulerAngles = new Vector3(0f, 0f, ItemArcMeasure * itemCount);
			EndBorder.localPosition = new Vector3(-0.5f, 0, 0);
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
