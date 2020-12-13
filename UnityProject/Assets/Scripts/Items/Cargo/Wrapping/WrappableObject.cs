using UnityEngine;

namespace Items.Cargo.Wrapping
{
	public class WrappableObject: WrappableBase
	{
		[SerializeField][Tooltip("Needed stacks of paper for wrapping this object.")]
		private int neededPaperAmount = 8;

		private RegisterCloset registerCloset;

		private void Awake()
		{
			registerCloset = GetComponent<RegisterCloset>();
		}

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
					Logger.LogError($"Tried to wrap {gameObject} with unknown type of paper", Category.Interaction);
					result = normalPackagePrefab;
					break;
			}

			result = Spawn.ServerPrefab(result, gameObject.AssumedWorldPosServer()).GameObject;
			var wrap = result.GetComponent<WrappedObject>();
			wrap.SetContent(gameObject);
			ContainerTypeSprite spriteType;

			switch (registerCloset.closetType)
			{
				case ClosetType.LOCKER:
					spriteType = ContainerTypeSprite.Locker;
					break;
				case ClosetType.CRATE:
					spriteType = ContainerTypeSprite.Crate;
					break;
				default:
					Logger.LogWarning($"{gameObject} is not a locker nor crate but it an attempt to wrap mas done." +
					                  "We set the crate sprite and go on.");
					spriteType = ContainerTypeSprite.Crate;
					break;
			}

			wrap.SetContainerTypeSprite(spriteType);
			Inventory.ServerConsume(paper.ItemSlot, neededPaperAmount);
		}
	}
}