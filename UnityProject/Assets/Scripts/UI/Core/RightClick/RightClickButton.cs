using UnityEngine.UI;

namespace UI.Core.RightClick
{
	/// <summary>
	/// The only purpose for this class is to expose the ability to instantly reset a button's state back to normal.
	/// </summary>
	public class RightClickButton : Button
	{
		public void ResetState() => InstantClearState();
	}
}