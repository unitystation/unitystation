public class ClientTab : Tab {
	public ClientTabType Type;
}

public enum ClientTabType {
	Stats = 0,
	More,
	ItemList,
	ControlInformation
	//add your tabs here
}