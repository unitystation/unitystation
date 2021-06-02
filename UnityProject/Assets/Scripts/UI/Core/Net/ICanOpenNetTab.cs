using UnityEngine;

//Called before server adds new player to netTab, this interface should be put on scripts on the provider
//NOT ON THE GUI script
namespace UI.Core.Net
{
	public interface ICanOpenNetTab
	{
		bool CanOpenNetTab(GameObject playerObject);
	}
}
