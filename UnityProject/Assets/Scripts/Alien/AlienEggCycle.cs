using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Random = System.Random;

namespace Alien
{
	public class AlienEggCycle : NetworkBehaviour, IInteractable<HandApply>
	{
		private const float OPENING_ANIM_TIME = 1.6f;
		private const int SMALL_SPRITE = 0;
		private const int BIG_SPRITE = 1;
		private const int OPENING_SPRITE = 2;
		private const int HATCHED_SPRITE = 3;
		private const int SQUISHED_SPRITE = 4;

		private float incubationTime = 300;
		[Tooltip("In what state will this egg spawn in the world.")]
		[SerializeField]
		private EggState initialState = EggState.Growing;

		[Tooltip("Allows mappers to have eggs that won't start cycle once spawned, but are still intractable")][SerializeField]
		private bool freezeCycle = false;
		[Tooltip("A reference for the facehugger mob so we can spawn it.")]
		[SerializeField]
		private GameObject facehugger = null;

		[SyncVar(hook = nameof(SyncState))]
		private EggState currentState;
		private SpriteHandler spriteHandler;
		private RegisterObject registerObject;
		private ObjectAttributes objectAttributes;
		public EggState State => currentState;

		private float serverTimer = 0f;
		private bool noLongerAlive = false;
		private bool initialized = false;
		private bool isUpdate = false;

		void EnsureInit()
		{
			if (initialized) return;
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			registerObject = GetComponent<RegisterObject>();
			objectAttributes = GetComponent<ObjectAttributes>();
			initialized = true;
		}

		private void OnDisable()
		{
			if (isUpdate)
			{
				isUpdate = false;
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			}
		}

		public override void OnStartClient()
		{
			base.OnStartClient();
			EnsureInit();
			SyncState(currentState, currentState);
		}

		public override void OnStartServer()
		{
			base.OnStartServer();
			EnsureInit();
			registerObject.WaitForMatrixInit(StartEggCycleServer);
		}

		void StartEggCycleServer(MatrixInfo info)
		{
			UpdateState(initialState);
			noLongerAlive = false;
			incubationTime = UnityEngine.Random.Range(60f, 400f);
			serverTimer = 0f;
			if (isUpdate)
			{
				isUpdate = true;
				UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			}
		}

		void UpdateMe()
		{
			if (freezeCycle || noLongerAlive) return;
			ServerMonitorEggState();
		}

		void ServerMonitorEggState()
		{
			serverTimer += Time.deltaTime;
			if (currentState == EggState.Growing)
			{
				if (serverTimer > incubationTime)
				{
					incubationTime = UnityEngine.Random.Range(60f, 400f);
					UpdateState(EggState.Grown);
					serverTimer = 0f;
				}
			}

			if (currentState == EggState.Grown)
			{
				if (serverTimer > incubationTime)
				{
					UpdateState(EggState.Burst);
					serverTimer = 0f;
				}
			}

			if (currentState == EggState.Burst)
			{
				if (serverTimer > OPENING_ANIM_TIME)
				{
					Spawn.ServerPrefab(facehugger, gameObject.RegisterTile().WorldPositionServer);
					noLongerAlive = true;
				}
			}
		}

		private void SyncState(EggState oldState, EggState newState)
		{
			EnsureInit();
			currentState = newState;

			switch (currentState)
			{
				case EggState.Growing:
					spriteHandler.SetSprite(spriteHandler.Sprites[SMALL_SPRITE]);
					break;
				case EggState.Grown:
					spriteHandler.SetSprite(spriteHandler.Sprites[BIG_SPRITE]);
					break;
				case EggState.Burst:
					StopAllCoroutines();
					StartCoroutine(OpenEgg());
					break;
				case EggState.Squished:
					spriteHandler.SetSprite(spriteHandler.Sprites[SQUISHED_SPRITE]);
					break;
			}
		}

		private IEnumerator OpenEgg()
		{
			spriteHandler.SetSprite(spriteHandler.Sprites[OPENING_SPRITE]);
			yield return WaitFor.Seconds(OPENING_ANIM_TIME);
			spriteHandler.SetSprite(spriteHandler.Sprites[HATCHED_SPRITE]);
		}

		private void Squish(HandApply interaction)
		{
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
			noLongerAlive = true;
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
			if (gameObject == null || !gameObject.activeInHierarchy)
			{
				return;
			}

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
