public class ClientTab : Tab {
	public ClientTabType Type;
}

public enum ClientTabType {
	Stats = 0,
	Options = 1,
	More = 2,
	ItemList = 3,
	//add your tabs here
}