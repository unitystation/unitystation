using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UI.Core;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class PlayerExaminationWindowSlot : MonoBehaviour
	{
		/// <summary>
		/// Object that will be enabled if slot is obscured
		/// </summary>
		[SerializeField] private GameObject obstructedOverlay = default;

		/// <summary>
		/// Object that will be enabled when player interacts and slot is obscured
		/// </summary>
		[SerializeField] private GameObject questionMark = default;

		[NonSerialized] public PlayerExaminationWindowUI parent;

		[SerializeField] private UI_ItemSlot itemSlot = default;
		public UI_ItemSlot UI_ItemSlot => itemSlot;

		public bool IsObscured => obstructedOverlay.activeSelf;

		public bool IsPocket => DynamicItemStorage.PocketSlots.Contains(itemSlot.NamedSlot)
		                        || itemSlot.NamedSlot == NamedSlot.suitStorage;

		public bool IsQuestionMarkActive => questionMark.activeSelf;

		public void Reset()
		{
			itemSlot.LinkSlot(null);
			itemSlot.Reset();
			obstructedOverlay.SetActive(false);
			questionMark.SetActive(false);
		}

		public void SetObscuredOverlayActive(bool active)
		{
			obstructedOverlay.SetActive(active);
		}

		public void SetQuestionMarkActive(bool active)
		{
			questionMark.SetActive(active);
		}

		public void OnClick()
		{
			parent.TryInteract(this);
		}

		public void SetUp(BodyPartUISlots.StorageCharacteristics StorageCharacteristics, ItemSlot ItemSlot,
			PlayerExaminationWindowUI toset)
		{
			UI_ItemSlot.LinkSlot(ItemSlot);
			UI_ItemSlot.SetUp(StorageCharacteristics);
			parent = toset;
		}

		public void RefreshImage()
		{
			UI_ItemSlot.RefreshImage();
		}
	}
}