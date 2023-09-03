using System;
using Logs;
using NaughtyAttributes;
using UnityEngine;

namespace Items.Cargo.Wrapping
{
	public class WrappableItem: WrappableBase
	{
		[SerializeField] [Tooltip("True if you want to set your own amount of paper.")]
		private bool overrideAmountOfPaper = false;

		[SerializeField,
		 ShowIf(nameof(overrideAmountOfPaper)),
		 Tooltip("How much paper should this object consume when wrapped.")]
		private int customAmountOfPaper = 5;

		[SerializeField] [Tooltip("Set to true if you want the package of this item to have a shape in particular.")]
		private bool shapedPackage = false;

		[SerializeField,
		 ShowIf(nameof(shapedPackage)),
		 Tooltip("Manually set the shape of the package this object will have once wrapped.")]
		private PackageType packageType = PackageType.Box;

		private ItemAttributesV2 itemAttributesV2;
		private Pickupable pickupable;


		private void Awake()
		{
			itemAttributesV2 = GetComponent<ItemAttributesV2>();
			pickupable = GetComponent<Pickupable>();
		}

		protected override bool CanBeWrapped(GameObject performer, WrappingPaper paper)
		{
			if (paper.PaperAmount >= GetPaperAmount())
			{
				return true;
			}

			Chat.AddExamineMsg(
				performer,
				$"It seems like I don't have enough {paper.gameObject.ExpensiveName()} to wrap {gameObject.ExpensiveName()}");

			return false;
		}

		protected override void Wrap(GameObject performer, WrappingPaper paper)
		{
			var cfg = new StandardProgressActionConfig(
				StandardProgressActionType.Restrain);

			Chat.AddActionMsgToChat(
				performer,
				string.Format(actionTextOriginator, gameObject.ExpensiveName(), paper.gameObject.ExpensiveName()),
				string.Format(actionTextOthers, performer.ExpensiveName(), gameObject.ExpensiveName(), paper.gameObject.ExpensiveName()));

			StandardProgressAction.Create(
				cfg,
				() => FinishWrapping(paper)
			).ServerStartProgress(ActionTarget.Object(performer.RegisterTile()), wrapTime, performer);
		}

		private void FinishWrapping(WrappingPaper paper)
		{
			GameObject result = null;

			switch (paper.WrapType)
			{
				case WrapType.Normal:
					result = normalPackagePrefab;
					break;
				case WrapType.Festive:
					result = festivePackagePrefab;
					break;
				default:
					Loggy.LogError($"Tried to wrap {gameObject} with unknown type of paper", Category.Cargo);
					result = normalPackagePrefab;
					break;
			}

			result = Spawn.ServerPrefab(result, gameObject.AssumedWorldPosServer()).GameObject;
			var wrap = result.GetComponent<WrappedItem>();
			var package = GetPackageType();
			wrap.SetSprite(package);
			wrap.SetSize(itemAttributesV2.Size);

			if (pickupable != null && pickupable.ItemSlot != null)
			{
				Inventory.ServerAdd(
					wrap.gameObject,
					pickupable.ItemSlot,
					ReplacementStrategy.DropOther);
			}

			wrap.SetContent(gameObject);
			Inventory.ServerConsume(paper.ItemSlot, GetPaperAmount());

		}

		private PackageType GetPackageType()
		{
			if (shapedPackage)
			{
				return packageType;
			}

			return (PackageType) itemAttributesV2.Size;
		}

		private int GetPaperAmount()
		{
			if (overrideAmountOfPaper)
			{
				return customAmountOfPaper;
			}

			return (int) itemAttributesV2.Size;
		}
	}
}