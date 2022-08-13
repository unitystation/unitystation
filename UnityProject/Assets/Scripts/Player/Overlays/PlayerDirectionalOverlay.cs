using Core.Transforms;
using UnityEngine;

namespace Effects.Overlays
{
	/// <summary>
	/// Handles animation of generic player overlay prefabs which have different sprites for each direction
	/// </summary>
	[RequireComponent(typeof(SpriteHandler))]
	public class PlayerDirectionalOverlay : MonoBehaviour
	{
		private SpriteHandler spriteHandler;

		public bool OverlayActive { get; private set; } = true; // PresentSpriteSO is set on startup

		private void Awake()
		{
			spriteHandler = GetComponent<SpriteHandler>();
		}

		private void Start()
		{
			StopOverlay();
		}

		private void OnDisable()
		{
			StopOverlay();
		}

		/// <summary>
		/// Display the overlay animation in the specified direction
		/// </summary>
		/// <param name="direction"></param>
		public void StartOverlay(OrientationEnum direction)
		{
			spriteHandler.ChangeSprite(0); // Load sprite into SpriteRenderer
			spriteHandler.ChangeSpriteVariant(GetOrientationVariant(direction));
			OverlayActive = true;
		}

		/// <summary>
		/// stop displaying the burning animation
		/// </summary>
		public void StopOverlay()
		{
			spriteHandler.PushClear();
			OverlayActive = false;
		}

		private int GetOrientationVariant(OrientationEnum orientation)
		{
			switch (orientation)
			{
				case OrientationEnum.Down_By180:
					return 0;
				case OrientationEnum.Up_By0:
					return 1;
				case OrientationEnum.Right_By270:
					return 2;
				case OrientationEnum.Left_By90:
					return 3;
			}

			return default;
		}
	}
}
