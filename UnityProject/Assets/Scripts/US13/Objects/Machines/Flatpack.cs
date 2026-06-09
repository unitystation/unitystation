using UnityEngine;
using US13.Core.Chat;
using US13.Core.Input_System.InteractionV2;
using US13.Core.Input_System.InteractionV2.Interactions;
using US13.Core.Input_System.InteractionV2.Interfaces;
using US13.Core.Lifecycle;
using US13.Core.Physics;
using US13.Core.Sprite_Handler;
using US13.Items;
using US13.Items.Tool;
using US13.Messages.Client.Interaction;
using US13.UI.Core.RightClick;
using Util;

namespace US13.Objects.Machines
{
	public enum Department
	{
		None = 0,
		Service = 1,
		Research = 2,
		Engineering = 3,
		Medical = 4,
		Security = 5,
	}

	public class Flatpack : MonoBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<ContextMenuApply>, IRightClickable
	{
		[field: SerializeField] public ObjectContainer objectContainer { get; private set; }
		[SerializeField] private SpriteHandler spriteHander = null;
		[SerializeField] private ItemAttributesV2 attributes = null;
		[SerializeField] private UniversalObjectPhysics objectPhysics = null;

		[SerializeField] private GameObject cardBoardPrefab = null;
		[SerializeField] private int amountToSpawn = 4;
		public void InitialiseType(Department department, string machineName)
		{
			spriteHander.SetSpriteVariant((int)department);
			if (department != Department.None)
				attributes.ServerSetArticleName($"{department} flatpack");

			attributes.ServerSetArticleDescription($"This is a flatpack. Contains various parts to your favourite machines." +
			                                       $"\nThis one contains a {machineName}" +
			                                       "\n*Some Assembly Required");
		}

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			ToolUtils.ServerUseToolWithActionMessages(interaction, 2f,
				$"You begin unpacking the {gameObject.ExpensiveName()}..",
				$"{interaction.Performer.ExpensiveName()} begins unpacking the {gameObject.ExpensiveName()}...",
				$"You unpack the {gameObject.ExpensiveName()}.",
				$"{interaction.Performer.ExpensiveName()} unpacks the {gameObject.ExpensiveName()}.",
				() =>
				{
					objectContainer.DropObjects();
					Spawn.ServerPrefab(cardBoardPrefab, objectPhysics.OfficialPosition, count: amountToSpawn);
					_ = Despawn.ServerSingle(this.gameObject);
				});
		}

		public RightClickableResult GenerateRightClickOptions()
		{
			var options = RightClickableResult.Create();

			options.AddElement("Open", OnOpenClicked);

			return options;
		}

		public void OnOpenClicked()
		{
			RequestInteractMessage.Send(ContextMenuApply.ByLocalPlayer(gameObject, "Open"), this);
		}

		public bool WillInteract(ContextMenuApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(ContextMenuApply interaction)
		{
			if (interaction.RequestedOption != "Open") return;

			Chat.AddActionMsgToChat(interaction.Performer, $"You begin unpacking the {gameObject.ExpensiveName()}..",
				$"{interaction.Performer.ExpensiveName()} begins unpacking the {gameObject.ExpensiveName()}...");

			ToolUtils.ServerUseTool(interaction.Performer, null, ActionTarget.Object(gameObject.RegisterTile()), 2f,
				() =>
				{
					Chat.AddActionMsgToChat(interaction.Performer, $"You unpack the {gameObject.ExpensiveName()}.",
						$"{interaction.Performer.ExpensiveName()} unpacks the {gameObject.ExpensiveName()}.");

					objectContainer.DropObjects();
					Spawn.ServerPrefab(cardBoardPrefab, objectPhysics.OfficialPosition, count: amountToSpawn);
					_ = Despawn.ServerSingle(this.gameObject);
				});
		}
	}
}
