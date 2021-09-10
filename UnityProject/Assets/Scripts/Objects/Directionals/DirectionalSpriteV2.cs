using UnityEngine;

namespace Core.Directionals
{
	/// <summary>
	/// Behaviour for an object which has a different sprite SO for each direction it is facing
	/// and changes facing when Directional tells it to.
	///
	/// Initial orientation should be set in Directional.
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Directional))]
	public class DirectionalSpriteV2 : MonoBehaviour
	{
		[Tooltip("Whether the sprite SO list should be changed instead of the variant, for some exceptional objects like morgues.")]
		[SerializeField]
		private bool isChangingSO = false;

		[Header("SpriteHandler Indexes")]
		[Tooltip("SpriteHandler index to use when facing down.")]
		[SerializeField]
		private int indexDown = 0;

		[Tooltip("SpriteHandler index to use when facing up.")]
		[SerializeField]
		private int indexUp = 1;

		[Tooltip("SpriteHandler index to use when facing right.")]
		[SerializeField]
		private int indexRight = 2;

		[Tooltip("SpriteHandler index to use when facing left.")]
		[SerializeField]
		private int indexLeft = 3;

		[Tooltip("The SpriteHandlers to control. Allows multiple for overlays.")]
		[SerializeField]
		private SpriteHandler[] spriteHandlers = default;

		private Directional directional;

		private void Awake()
		{
			directional = GetComponent<Directional>();
		}

		private void Start()
		{
			if (Application.isEditor)
			{
				SetSpriteOrientation(directional.InitialOrientation);
			}
			else
			{
				SetSpriteOrientation(directional.CurrentDirection);
			}
		}

		private void OnEnable()
		{
			directional.OnDirectionChange.AddListener(OnDirectionChanged);
			directional.onEditorDirectionChange.AddListener(OnEditorDirectionChanged);
		}

		private void OnDisable()
		{
			directional.OnDirectionChange.RemoveListener(SetSpriteOrientation);
			directional.onEditorDirectionChange.RemoveListener(OnEditorDirectionChanged);
		}

		private void OnDirectionChanged(Orientation newOrientation)
		{
			SetSpriteOrientation(newOrientation);
		}

		private void OnEditorDirectionChanged()
		{
			SetSpriteOrientation(directional.InitialOrientation);
		}

		private void SetSpriteOrientation(Orientation newOrientation)
		{
			int index = GetIndexFromOrientation(newOrientation);

			if (isChangingSO)
			{
				foreach (var handler in spriteHandlers)
				{
					handler.ChangeSprite(index);
				}
			}
			else
			{
				foreach (var handler in spriteHandlers)
				{
					handler.ChangeSpriteVariant(index);
				}
			}
		}

		private int GetIndexFromOrientation(Orientation orientation)
		{
			switch (orientation.AsEnum())
			{
				case OrientationEnum.Down:
					return indexDown;
				case OrientationEnum.Up:
					return indexUp;
				case OrientationEnum.Right:
					return indexRight;
				case OrientationEnum.Left:
					return indexLeft;
			}

			return default;
		}
	}
}
