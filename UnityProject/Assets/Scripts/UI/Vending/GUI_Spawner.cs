public class GUI_Spawner : NetUITab 
{
	//todo: add / delete
	public void AddItem( string item ) {
		EntryList?.AddItem( item );
	}
	public void RemoveItem( string item ) {
		EntryList?.RemoveItem( item );
	}
	private ItemList EntryList => Info["EntryList"] as ItemList;
	private void Start() {
		//Not doing this for clients
		if ( CustomNetworkManager.Instance._isServer ) {
//			Debug.Log( $"{name} Kinda init. Nuke code is {NukeInteract.NukeCode}" );
//			InitialInfoText = $"Enter {NukeInteract.NukeCode.ToString().Length}-digit code:";
//			InfoDisplay.SetValue = InitialInfoText;
		}
	}
}
