using System;
using System.Linq;
using Objects;
using UnityEngine;

namespace Items.Cargo.Wrapping
{
	public class WrappedObject: WrappedBase, ICheckedInteractable<HandApply>, IServerSpawn, IEscapable
	{
		[SerializeField]
		[Tooltip("Use this to set the initial type of this object. " +
		         "The sprite will change to represent this change when the object is spawned. " +
		         "Useful for mapping!")]
		private ContainerTypeSprite typeSprite;

		public override void UnWrap()
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
				_ = Despawn.ServerSingle(gameObject);
				return;
			}

			MakeContentVisible(unwrapped);//Does what it says on the tin

			//Remove the container which was being wrapped from storage
			RetrieveObject(unwrapped,gameObject.AssumedWorldPosServer());

			//Return the contents back to the wrapped container
			TransferObjectsTo(unwrapped.GetComponent<ObjectContainer>());
			_ = Despawn.ServerSingle(gameObject);
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

		public override void OnSpawnServer(SpawnInfo info)
		{
			base.OnSpawnServer(info);

			if (info.SpawnType != SpawnType.Mapped) return;
			SetContainerTypeSprite(typeSprite);
		}

		public void EntityTryEscape(GameObject entity, Action ifCompleted, MoveAction moveAction)
		{
			var container = GetOrGenerateContent();
			container.GetComponent<IEscapable>().EntityTryEscape(entity, () =>
			{
				//The entity can have a little freedom, as a treat
				RetrieveObject(entity, gameObject.AssumedWorldPosServer());

				UnWrap();

				//A successful escape assumes the container is now open, thus items must be released.
				container.GetComponent<ObjectContainer>().RetrieveObjects();
			}, moveAction);
		}
	}

	public enum ContainerTypeSprite
	{
		Crate,
		Locker
	}
}
