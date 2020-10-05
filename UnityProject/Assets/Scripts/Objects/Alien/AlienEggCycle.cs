using System;
using System.Collections;
using UnityEngine;
using Mirror;

namespace Alien
{
	public class AlienEggCycle : MonoBehaviour, IInteractable<HandApply>, IServerSpawn, IServerDespawn
	{
		private const float OPENING_ANIM_TIME = 1.6f;
		private const int SMALL_SPRITE = 0;
		private const int BIG_SPRITE = 1;
		private const int OPENING_SPRITE = 2;
		private const int HATCHED_SPRITE = 3;
		private const int SQUISHED_SPRITE = 4;


		[Tooltip("The spawned egg will take a random amount of time from 60 seconds to this attribute to spawn " +
		         "a facehugger.")]
		[SerializeField]
		private float incubationTime = 300;

		[Tooltip("In what state will this egg spawn in the world.")]
		[SerializeField]
		private EggState initialState = EggState.Growing;

		// TODO This is entirely unused and is creating a compiler warning.
		[Tooltip("Allows mappers to have eggs that won't start cycle once spawned, but are still intractable")][SerializeField]
		private bool freezeCycle = false;

		[Tooltip("A reference for the facehugger mob so we can spawn it.")]
		[SerializeField]
		private GameObject facehugger = null;

		private EggState currentState;
		private SpriteHandler spriteHandler;
		private RegisterObject registerObject;
		private ObjectAttributes objectAttributes;
		public EggState State => currentState;


		public void Awake()
		{
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			registerObject = GetComponent<RegisterObject>();
			objectAttributes = GetComponent<ObjectAttributes>();
		}


		public void OnSpawnServer(SpawnInfo info)
		{
			incubationTime = UnityEngine.Random.Range(60f, incubationTime);
			UpdatePhase(initialState);
			UpdateExamineMessage();
			registerObject.Passable = false;
		}


		public void OnDespawnServer(DespawnInfo info)
		{
			StopAllCoroutines();
		}


		private void UpdatePhase(EggState state)
		{
			currentState = state;

			switch (currentState)
			{
				case EggState.Growing:
					spriteHandler.ChangeSprite(SMALL_SPRITE);
					StopAllCoroutines();
					StartCoroutine(GrowEgg());
					break;
				case EggState.Grown:
					spriteHandler.ChangeSprite(BIG_SPRITE);
					StopAllCoroutines();
					StartCoroutine(HatchEgg());
					break;
				case EggState.Burst:
					spriteHandler.ChangeSprite(HATCHED_SPRITE);
					registerObject.Passable = true;
					break;
				case EggState.Squished:
					spriteHandler.ChangeSprite(SQUISHED_SPRITE);
					break;
			}

			UpdateExamineMessage();
		}


		IEnumerator GrowEgg()
		{
			yield return WaitFor.Seconds(incubationTime / 2);
			UpdatePhase(EggState.Grown);
		}


		IEnumerator HatchEgg()
		{
			yield return WaitFor.Seconds(incubationTime / 2);
			spriteHandler.ChangeSprite(OPENING_SPRITE);
			yield return WaitFor.Seconds(OPENING_ANIM_TIME);
			UpdatePhase(EggState.Burst);

			Spawn.ServerPrefab(facehugger, gameObject.RegisterTile().WorldPositionServer);
		}


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
					UpdatePhase(EggState.Burst);
					break;
				default:
					UpdatePhase(EggState.Burst);
					break;
			}
		}


		private void Squish(HandApply interaction)
		{
			StopAllCoroutines();

			SoundManager.PlayNetworkedAtPos(
				"squish",
				gameObject.RegisterTile().WorldPositionServer,
				1f,
				global: false);

			Chat.AddActionMsgToChat(
				interaction.Performer.gameObject,
				"You squish the alien egg!",
				$"{interaction.Performer.ExpensiveName()} squishes the alien egg!");

			UpdatePhase(EggState.Squished);
			registerObject.Passable = true;
		}


		private void FeelsSlimy(Interaction interaction)
		{
			Chat.AddActionMsgToChat(
				interaction.Performer,
				"It feels slimy",
				$"{interaction.Performer.ExpensiveName()} touches the slimy egg!");
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
