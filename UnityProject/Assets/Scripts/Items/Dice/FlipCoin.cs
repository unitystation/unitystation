using System.Collections;
using UnityEngine;

/// <summary>
/// Allows a coin to be flipped by activating in-hand or throwing.
/// </summary>
public class FlipCoin : MonoBehaviour, IExaminable, ICheckedInteractable<HandActivate>
{
	private const float FLIP_TIME = 1; // In seconds.

	private const int SPRITE_RESTING = 0;
	private const int SPRITE_FLIPPING = 1;
	private const int SPRITE_VARIANT_RESTING_UPRIGHT = 0;
	private const int SPRITE_VARIANT_RESTING_INVERTED = 1;

	[SerializeField]
	[Tooltip("The name of the top face. This is the default, upright position.")]
	private string headName = "head";

	[SerializeField]
	[Tooltip("The name of the bottom face.")]
	private string tailName = "tail";

	private SpriteHandler spriteHandler;
	private UniversalObjectPhysics universalObjectPhysics;

	public bool IsUpright { get; private set; } = true;

	#region Lifecycle

	private void Awake()
	{
		spriteHandler = GetComponentInChildren<SpriteHandler>();
		universalObjectPhysics = GetComponent<UniversalObjectPhysics>();
	}

	private void OnEnable()
	{
		universalObjectPhysics.OnThrowStart.AddListener(ThrowStart);
		universalObjectPhysics.OnImpact.AddListener(ThrowEnd);
	}

	private void OnDisable()
	{
		universalObjectPhysics.OnThrowStart.RemoveListener(ThrowStart);
		universalObjectPhysics.OnImpact.RemoveListener(ThrowEnd);
	}

	#endregion Lifecycle

	private void UpdateSprite()
	{
		spriteHandler.ChangeSprite(SPRITE_RESTING);
		spriteHandler.ChangeSpriteVariant(IsUpright ? SPRITE_VARIANT_RESTING_UPRIGHT : SPRITE_VARIANT_RESTING_INVERTED);
	}

	private string GetFaceName()
	{
		return IsUpright ? headName : tailName;
	}

	#region Interactions

	public virtual string Examine(Vector3 worldPos = default)
	{
		return $"It is showing face '{GetFaceName()}'.";
	}

	public bool WillInteract(HandActivate interaction, NetworkSide side)
	{
		return DefaultWillInteract.Default(interaction, side);
	}

	public void ServerPerformInteraction(HandActivate interaction)
	{
		IsUpright = !IsUpright;
		UpdateSprite();
	}

	#endregion Interactions

	#region Throwing

	private void ThrowStart(UniversalObjectPhysics throwInfo)
	{
		if (throwInfo.thrownBy != null)
		{
			Chat.AddActionMsgToChat(throwInfo.thrownBy, $"You throw the coin...", $"{throwInfo.thrownBy} throws the coin...");
		}
	}

	private void ThrowEnd(UniversalObjectPhysics throwInfo, Vector2 Force)
	{
		if (Force.magnitude > 0.5f)
		{
			StartCoroutine(Flip());
		}

	}

	private IEnumerator Flip()
	{
		// The flip animation sprite SO has only only one variant.
		spriteHandler.ChangeSpriteVariant(0);
		spriteHandler.ChangeSprite(SPRITE_FLIPPING);
		yield return WaitFor.Seconds(FLIP_TIME);

		IsUpright = Random.Range(0, 2) == 0;
		UpdateSprite();

		Chat.AddActionMsgToChat(gameObject, $"The coin lands a '{GetFaceName()}'.");
	}

	#endregion Throwing
}
