using System;
using Chemistry.Components;
using Logs;
using UnityEngine;

namespace Effects.FloorEffect
{
	public class FloorPrintEffect : MonoBehaviour
	{
		//TODO In future, Ask what type of  print Then change the sprite to that, Doesn't matter about overwriting

		public SpriteHandler[] EnterHandlers;
		public SpriteHandler[] ExitHandlers;
		public ReagentContainer ReagentMix;

		[SerializeField] private float reagentMixMultiplierAlphaCheck = 2f;

		public void Awake()
		{
			ReagentMix = this.GetComponent<ReagentContainer>();
		}

		public void RegisterEnter(OrientationEnum localOrientation)
		{
			if (localOrientation == OrientationEnum.Default)
			{
				//(Max): average british insult
				Loggy.LogError("[FloorPrintEffect/RegisterEnter] - Tried to pass OrientationEnum.Default to Footprints what are you thinking you numpty,  Defaulting to up");
				localOrientation = OrientationEnum.Up_By0;
			}
			var colour = ReagentMix.CurrentReagentMix.MixColor;

			colour.a = Mathf.Clamp(Mathf.Lerp(0.15f, 1f, ReagentMix.CurrentReagentMix.Total * reagentMixMultiplierAlphaCheck) ,0.15f, 1);
			EnterHandlers[(int)localOrientation].PushTexture();
			EnterHandlers[(int)localOrientation].SetColor(colour);
		}

		public void RegisterLeave(OrientationEnum localOrientation)
		{
			if (localOrientation == OrientationEnum.Default)
			{
				Loggy.LogError("[FloorPrintEffect/RegisterLeave] - Tried to pass OrientationEnum.Default to Footprints what are you thinking you numpty,  Defaulting to up");
				localOrientation = OrientationEnum.Up_By0;
			}

			var colour = ReagentMix.CurrentReagentMix.MixColor;

			colour.a = Mathf.Clamp(Mathf.Lerp(0.15f, 1f, ReagentMix.CurrentReagentMix.Total*10) ,0.15f, 1);
			ExitHandlers[(int)localOrientation].PushTexture();
			ExitHandlers[(int)localOrientation].SetColor(colour);
		}
	}
}
