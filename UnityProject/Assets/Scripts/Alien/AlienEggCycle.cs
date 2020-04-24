using System;
using System.Collections;
using Mirror;
using UnityEngine;

namespace Alien
{
	public class AlienEggCycle : NetworkBehaviour, IInteractable<HandApply>, IServerSpawn
	{
		private const float OPENING_ANIM_TIME = 1.6f;
		private const int SMALL_SPRITE = 0;
		private const int BIG_SPRITE = 1;
		private const int OPENING_SPRITE = 2;
		private const int HATCHED_SPRITE = 3;
		private const int SQUISHED_SPRITE = 4;

		[Tooltip("How much  time should the egg take incubating the facehugger. This includes from the growing phase until the actual spawn")]
		[SerializeField]
		private float incubationTime = 300;
		[Tooltip("In what state will this egg spawn in the world.")]
		[SerializeField]
		private EggState initialState = EggState.Growing;

		[Tooltip("Allows mappers to have eggs that won't start cycle once spawned, but are still intractable")][SerializeField]
		private bool freezeCycle = false;
		[Tooltip("A reference for the facehugger mob so we can spawn it.")]
		[SerializeField]
		private GameObject facehugger;

		[SyncVar(hook = nameof(SyncState))]
		private EggState currentState;
		private SpriteHandler spriteHandler;
		private RegisterObject registerObject;
		private ObjectAttributes objectAttributes;
		public EggState State => currentState;

		private void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			registerObject = GetComponent<RegisterObject>();
			objectAttributes = GetComponent<ObjectAttributes>();

			EnsureInit();
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			if (isServer)
			{
				UpdateState(initialState);
			}

			SyncState(currentState, currentState);
		}

		private void SyncState(EggState oldState, EggState newState)
		{
			currentState = newState;

			switch (currentState)
			{
				case EggState.Growing:
					StopAllCoroutines();
					StartCoroutine(nameof(StartIncubation));
					break;
				case EggState.Grown:
					StopAllCoroutines();
					StartCoroutine(nameof(FinishIncubation));
					break;
				case EggState.Burst:
					StopAllCoroutines();
					StartCoroutine(nameof(OpenEgg));
					break;
				case EggState.Squished:
					spriteHandler.SetSprite(spriteHandler.Sprites[SQUISHED_SPRITE]);
					break;
			}
		}

		private IEnumerator StartIncubation()
		{
			spriteHandler.SetSprite(spriteHandler.Sprites[SMALL_SPRITE]);

			if (freezeCycle)
			{
				yield break;
			}

			yield return WaitFor.Seconds(incubationTime / 2);
			UpdateState(currentState == EggState.Growing ? EggState.Grown : currentState);
		}

		private IEnumerator FinishIncubation()
		{
			spriteHandler.SetSprite(spriteHandler.Sprites[BIG_SPRITE]);

			if (freezeCycle)
			{
				yield break;
			}
			
			yield return WaitFor.Seconds(incubationTime / 2);
			UpdateState(currentState == EggState.Grown ? EggState.Burst : currentState);
		}

		private IEnumerator OpenEgg()
		{
			spriteHandler.SetSprite(spriteHandler.Sprites[OPENING_SPRITE]);
			yield return WaitFor.Seconds(OPENING_ANIM_TIME);
			spriteHandler.SetSprite(spriteHandler.Sprites[HATCHED_SPRITE]);
			Spawn.ServerPrefab(facehugger, gameObject.RegisterTile().WorldPositionServer);
		}

		private void Squish(HandApply interaction)
		{
			StopAllCoroutines();
			SoundManager.PlayNetworkedAtPos(
				"squish",
				gameObject.RegisterTile().WorldPositionServer,
				1f,
				Global: false);

			Chat.AddActionMsgToChat(
				interaction.Performer.gameObject,
				"You squish the alien egg!",
				$"{interaction.Performer.ExpensiveName()} squishes the alien egg!");

			UpdateState(EggState.Squished);
			registerObject.Passable = true;
		}

		[Server]
		private void UpdateExamineMessage()
		{
			string examineMessage = objectAttributes.InitialDescription;

			switch (currentState)
			{
				case EggState.Growing:
					examineMessage = "A small mottled egg";
					break;
				case EggState.Grown:
					examineMessage = "A large mottled egg. Something is moving inside it...";
					break;
				case EggState.Burst:
					examineMessage = "A large mottled egg. Doesn't look like it is moving...";
					break;
				case EggState.Squished:
					examineMessage = "A small mottled egg or so it was.";
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			objectAttributes.ServerSetArticleDescription(examineMessage);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			switch (currentState)
			{
				case EggState.Squished:
					FeelsSlimy(interaction);
					break;
				case EggState.Burst:
					FeelsSlimy(interaction);
					break;
				case EggState.Growing:
					Squish(interaction);
					break;
				case EggState.Grown:
					UpdateState(EggState.Burst);
					break;
				default:
					UpdateState(EggState.Burst);
					break;
			}
		}

		private void FeelsSlimy(Interaction interaction)
		{
			Chat.AddActionMsgToChat(
				interaction.Performer,
				"It feels slimy",
				$"{interaction.Performer.ExpensiveName()} touches the slimy egg!");
		}

		[Server]
		private void UpdateState(EggState state)
		{
			if (state == currentState)
			{
				UpdateExamineMessage();
				return;
			}

			currentState = state;
			UpdateExamineMessage();
		}

		public enum EggState
		{
			Growing,
			Grown,
			Burst,
			Squished
		}
	}
}
