using UnityEngine;
using US13.Core.Sprite_Handler;
using US13.Core.Utils;
using US13.Items.Implants.Organs;

namespace BodyParts
{
	public class TongueSprites : MonoBehaviour
	{

		public SpriteHandler  SpriteHandler;


		public SpriteDataSO MouthAnimation;
		public SpriteDataSO Blank;


		public MultiInterestBool IsTalking = new MultiInterestBool();

		public void OnEnable()
		{

			IsTalking.OnBoolChange.AddListener(SwapTalking);
		}

		public void SwapTalking(bool val)
		{


			if (val)
			{
				if (Tongue.SpeechAnimationEnabled == false) return;

				SpriteHandler.SetSpriteSO(MouthAnimation, networked: false);

			}
			else
			{
				SpriteHandler.SetSpriteSO(Blank, networked: false);
			}


		}

	}

}
