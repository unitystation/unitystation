using UnityEngine;
using Objects;

namespace Items.Cargo.Wrapping
{
	public class WrappableObject: WrappableBase
	{
		[SerializeField]
		[Tooltip("Needed stacks of paper for wrapping this object.")]
		private int neededPaperAmount = 8;

		[SerializeField]
		[Tooltip("Type of wrapped sprite to use for this container.")]
		private ContainerTypeSprite spriteType = ContainerTypeSprite.Crate;

		protected override bool CanBeWrapped(GameObject performer, WrappingPaper paper)
		{
			if (paper.PaperAmount >= neededPaperAmount)
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
			GameObject result;

			switch (paper.WrapType)
			{
				case WrapType.Normal:
					result = normalPackagePrefab;
					break;
				case WrapType.Festive:
					result = festivePackagePrefab;
					break;
				default:
					Logger.LogError($"Tried to wrap {gameObject} with unknown type of paper", Category.Cargo);
					result = normalPackagePrefab;
					break;
			}

			result = Spawn.ServerPrefab(result, gameObject.AssumedWorldPosServer()).GameObject;
			var wrap = result.GetComponent<WrappedObject>();
			wrap.SetContent(gameObject);

			wrap.SetContainerTypeSprite(spriteType);
			Inventory.ServerConsume(paper.ItemSlot, neededPaperAmount);
		}
	}
}
