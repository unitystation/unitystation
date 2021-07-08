using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// Hides "unpressed" image when button is pressed 
public class ToggleButton : Toggle {
	
	protected override void Awake() {
		onValueChanged.AddListener( SwapGraphics() );
	}

	private UnityAction<bool> SwapGraphics() {
		return b => PlayEffectPressed(toggleTransition == ToggleTransition.None);
	}

	private void PlayEffectPressed( bool instant = false ) {
		if ( targetGraphic == null ) {
			return;
		}

		if ( !Application.isPlaying ) {
			targetGraphic.canvasRenderer.SetAlpha(!isOn ? 1f : 0f);
		} else {
			targetGraphic.CrossFadeAlpha(!isOn ? 1f : 0f, instant ? 0f : 0.2f, true);
		}
	}

	protected override void Start() {
		base.Start();
		PlayEffectPressed( true );
	}

	protected override void OnEnable() {
		base.OnEnable();
		PlayEffectPressed( true );
	}
}