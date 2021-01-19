using UnityEngine;

namespace Items.Cargo.Wrapping
{
	public class WrappedObject: WrappedBase, ICheckedInteractable<HandApply>, IServerSpawn
	{
		[SerializeField]
		[Tooltip("Use this to set the initial type of this object. " +
		         "The sprite will change to represent this change when the object is spawned. " +
		         "Useful for mapping!")]
		private ContainerTypeSprite typeSprite;
		protected override void UnWrap()
		{
			PlayUnwrappingSound();
			var unwrapped = GetOrGenerateContent();
			if (unwrapped == null)
			{
				Chat.AddActionMsgToChat(
					gameObject,
					"",
					$"The {gameObject.ExpensiveName()} finishes unwrapping itself but there is no content! " +
					$"What is this magic?!");
				Despawn.ServerSingle(gameObject);
				return;
			}

			MakeContentVisible();
			Despawn.ServerSingle(gameObject);
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side) &&
			       interaction.TargetObject == gameObject &&
			       interaction.HandObject == null &&
			       interaction.Intent == Intent.Harm;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			StartUnwrapAction(interaction.Performer);
		}

		public void SetContainerTypeSprite(ContainerTypeSprite type)
		{
			spriteHandler.ChangeSprite((int) type);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (info.SpawnType != SpawnType.Mapped) return;
			SetContainerTypeSprite(typeSprite);
		}
	}

	public enum ContainerTypeSprite
	{
		Crate,
		Locker
	}
}