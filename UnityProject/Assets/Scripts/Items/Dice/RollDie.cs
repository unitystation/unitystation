using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

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
	private CustomNetTransform netTransform;
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
		netTransform = GetComponent<CustomNetTransform>();
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
		netTransform.OnThrowStart.AddListener(ThrowStart);
		netTransform.OnThrowEnd.AddListener(ThrowEnd);

		if (cookable != null && isRiggable)
		{
			cookable.OnCooked += Cook;
		}
	}

	private void OnDisable()
	{
		netTransform.OnThrowStart.RemoveListener(ThrowStart);
		netTransform.OnThrowEnd.RemoveListener(ThrowEnd);

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

	private void ThrowStart(ThrowInfo throwInfo)
	{
		if(throwInfo.ThrownBy.GetComponent<NetworkIdentity>() == null) return;

		Chat.AddActionMsgToChat(throwInfo.ThrownBy, $"You throw the {dieName}...", $"{throwInfo.ThrownBy.ExpensiveName()} throws the {dieName}...");
	}

	private void ThrowEnd(ThrowInfo throwInfo)
	{
		this.RestartCoroutine(WaitForSide(), ref waitForSide);
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
		Chat.AddLocalMsgToChat(GetMessage(), gameObject);
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
			if (GetProbability(Mathf.Clamp((1 / sides) * 100, 25, 80)))
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

	/// <summary>
	/// Gets the probability of a given value being within 0 and a number generated from 0 to 100.
	/// </summary>
	/// <param name="percent">The percentage value to check with</param>
	/// <returns>True if the given value was less or equal to a random number between 0 and 100</returns>
	protected bool GetProbability(int percent)
	{
		return Random.Range(0, 100) <= percent;
	}
}
