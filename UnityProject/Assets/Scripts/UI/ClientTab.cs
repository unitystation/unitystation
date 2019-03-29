public class ClientTab : Tab {
	public ClientTabType Type;
}

public enum ClientTabType {
	Stats = 0,
	More,
	ItemList,
	ControlInformation,
	Dev
	//add your tabs here
}