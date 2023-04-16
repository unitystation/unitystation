using System;
using System.Collections;
using Mirror;
using UnityEngine;
using Items.Food;
using Random = UnityEngine.Random;

namespace Items.Dice
{
	/// <summary>
	/// An extendable MonoBehaviour component to handle generic die rolling.
	/// </summary>
	public class RollDie : MonoBehaviour, IExaminable, ICheckedInteractable<HandActivate>
	{
		[Tooltip("The amount of sides this die has.")]
		public int sides = 6;
		[Tooltip("Whether the die can be rigged to give flawed results.")]
		public bool isRiggable = true;

		private Transform dieTransform;
		private SpriteHandler faceOverlayHandler;
		private UniversalObjectPhysics ObjectPhysics ;
		private Cookable cookable;

		private const float ROLL_TIME = 1; // In seconds.
		protected string dieName = "die";
		protected bool isRigged = false;
		protected int riggedValue;
		protected int result;

		public bool IsRolling { get; private set; } = false;

		protected HandActivate handRollInteraction;

		private Coroutine waitForSide;
		private Coroutine animateSides;
		private Coroutine animateBounce;

		#region Lifecycle

		protected virtual void Awake()
		{
			dieTransform = transform;
			// This assumes that the Face GameObject, responsible for handling the dice sprite overlays,
			// is in the second position of the dice hierarchy.
			faceOverlayHandler = transform.GetChild(1).GetComponent<SpriteHandler>();
			ObjectPhysics = GetComponent<UniversalObjectPhysics>();
			cookable = GetComponent<Cookable>();
		}

		protected virtual void Start()
		{
			dieName = gameObject.ExpensiveName();

			// Start the die with a random side.
			result = Random.Range(1, sides);
			UpdateOverlay();
		}

		private void OnEnable()
		{
			ObjectPhysics.OnThrowStart.AddListener(ThrowStart);
			ObjectPhysics.OnImpact.AddListener(ThrowEnd);
			ObjectPhysics.OnThrowEnd.AddListener(ThrowEndOld);
			if (cookable != null && isRiggable)
			{
				cookable.OnCooked += Cook;
			}
		}

		private void OnDisable()
		{
			ObjectPhysics.OnThrowStart.RemoveListener(ThrowStart);
			ObjectPhysics.OnImpact.RemoveListener(ThrowEnd);
			ObjectPhysics.OnThrowEnd.RemoveListener(ThrowEndOld);
			if (cookable != null)
			{
				cookable.OnCooked -= Cook;
			}
		}

		#endregion Lifecycle

		#region Interactions

		public bool WillInteract(HandActivate interaction, NetworkSide side)
		{
			return DefaultWillInteract.Default(interaction, side);
		}

		public void ServerPerformInteraction(HandActivate interaction)
		{
			handRollInteraction = interaction;

			StartCoroutine(RollInHand());
		}

		public virtual string Examine(Vector3 worldPos = default)
		{
			return $"It is showing side {result}.";
		}

		#endregion Interactions

		public void Cook()
		{
			if (isRiggable)
			{
				isRigged = true;
				riggedValue = result;
			}
		}

		private IEnumerator RollInHand()
		{
			Chat.AddExamineMsgFromServer(handRollInteraction.Performer, $"You roll the {dieName} in your hands...");

			IsRolling = true;
			this.RestartCoroutine(AnimateSides(), ref animateSides);
			yield return WaitFor.Seconds(ROLL_TIME);
			IsRolling = false;

			result = GetSide();
			UpdateOverlay();
			Chat.AddExamineMsgFromServer(handRollInteraction.Performer, GetMessage());
		}

		#region Throwing

		private void ThrowStart(UniversalObjectPhysics throwInfo)
		{
			if (throwInfo.thrownBy.OrNull()?.GetComponent<NetworkIdentity>() == null) return;

			Chat.AddActionMsgToChat(throwInfo.thrownBy, $"You throw the {dieName}...", $"{throwInfo.thrownBy.ExpensiveName()} throws the {dieName}...");
		}

		private void ThrowEndOld(UniversalObjectPhysics throwInfo)
		{
			this.RestartCoroutine(WaitForSide(), ref waitForSide);
		}

		private void ThrowEnd(UniversalObjectPhysics throwInfo, Vector2 Force)
		{
			if (Force.magnitude > 0.01f)
			{
				this.RestartCoroutine(WaitForSide(), ref waitForSide);
			}

		}

		private IEnumerator WaitForSide()
		{
			IsRolling = true;
			this.RestartCoroutine(AnimateSides(), ref animateSides);
			this.RestartCoroutine(AnimateBounce(), ref animateBounce);
			yield return WaitFor.Seconds(ROLL_TIME);
			IsRolling = false;

			result = GetSide();
			UpdateOverlay();
			Chat.AddActionMsgToChat(gameObject, GetMessage());
		}

		#endregion Throwing

		#region Animations

		protected IEnumerator AnimateSides()
		{
			while (IsRolling)
			{
				faceOverlayHandler.ChangeSpriteVariant(Random.Range(0, sides));
				yield return WaitFor.Seconds(0.1f);
			}
		}

		protected IEnumerator AnimateBounce()
		{
			// Rotate some limited angle from north, bounce in that direction, return to original position.
			// TODO: Pretty bad bounce. Some lerping would help.
			while (IsRolling)
			{
				dieTransform.Rotate(new Vector3(0, 0, Random.Range(-180, 180)), Space.World);
				dieTransform.Translate(new Vector3(0, 0.2f, 0));
				yield return WaitFor.Seconds(0.1f);

				dieTransform.Translate(new Vector3(0, -0.2f, 0));
				yield return WaitFor.Seconds(0.1f);
			}
		}

		#endregion Animations

		private void UpdateOverlay()
		{
			transform.Rotate(Vector3.zero, Space.World);
			faceOverlayHandler.ChangeSpriteVariant(result - 1);
		}

		protected virtual int GetSide()
		{
			int result = Random.Range(1, sides + 1);

			if (isRigged && result != riggedValue)
			{
				if (DMMath.Prob(Mathf.Clamp((1 / sides) * 100, 25, 80)))
				{
					return riggedValue;
				}
			}

			return result;
		}

		protected virtual string GetMessage()
		{
			return $"The {dieName} lands a {result}.";
		}
	}
}
