public class ClientTab : Tab {
	public ClientTabType Type;
}

public enum ClientTabType {
	Stats = 0,
	More,
	ItemList,
	ControlInformation,
	Admin
	//add your tabs here
}