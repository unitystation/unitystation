using UnityEngine;

namespace Learning
{
	public class ProtipManager : Managers.SingletonManager<ProtipManager>
	{
		public ProtipUI UI;


		//TODO : ADD TIP QUEUEING

		public void ShowTip(string TipText, float duration = 25f, Sprite img = null, ProtipUI.SpriteAnimation showAnim = ProtipUI.SpriteAnimation.ROCKING)
		{
			UI.ShowTip(TipText, duration, img, showAnim);
		}
	}
}