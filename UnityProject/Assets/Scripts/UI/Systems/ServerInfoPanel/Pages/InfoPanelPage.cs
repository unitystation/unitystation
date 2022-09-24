using UnityEngine;

namespace UI.Systems.ServerInfoPanel
{
	public abstract class InfoPanelPage: MonoBehaviour
	{
		public abstract bool HasContent();
	}
}