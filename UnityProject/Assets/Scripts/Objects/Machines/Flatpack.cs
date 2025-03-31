using Core.Physics;
using Items;
using UnityEngine;

namespace Objects
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

	public class Flatpack : MonoBehaviour, ICheckedInteractable<HandActivate>, ICheckedInteractable<HandApply>
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

		public bool WillInteract(HandApply interaction, NetworkSide side)
        {
        		return DefaultWillInteract.Default(interaction, side) && interaction.IsAltClick;
        }

        public void ServerPerformInteraction(HandApply interaction)
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
	}
}
