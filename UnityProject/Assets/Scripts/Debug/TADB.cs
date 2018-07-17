using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public enum Category{
	Atmospherics,
	Movement,
	MapLoad, 
	RoomControl,
	ItemList,
	ItemEntry,
	DmiIconData,
	UI,
	MatrixManager,
	Equipment,
	NetworkManager,
	uniCloth,
	NetworkTabManager,
	ItemFactory,
	Light2D,
	PlayerList,
	PoolManager,
	SpriteManager,
	PlayerNetworkActions,
	SteamIntegration,
	TabUISystem,
	Telecommunications,
	PlayerJobManager,
	PushPull,
	NetSpriteImage,
	ShutterController,
	ShipNavigation,
	Lighting,
	Electrical, 
	Unknown,
};

public class Logger
	{

	//-1 will not show anything
	// 0 Only will show errors 
	// 1 will show errors and warnings
	// 2 will show errors, warnings and logs
	private static Dictionary<Category, int> FilterDictionary = new Dictionary<Category, int>{
		[Category.Atmospherics] = 0,
		[Category.Movement] = 0,
		[Category.MapLoad] = 0,
		[Category.RoomControl] = 0,
		[Category.ItemList] = 0,
		[Category.ItemEntry] = 0,
		[Category.DmiIconData] = 0,
		[Category.UI] = 0,
		[Category.MatrixManager] = 0,
		[Category.Equipment] = 0,
		[Category.NetworkManager] = 0,
		[Category.uniCloth] = 0,
		[Category.NetworkTabManager] = 0,
		[Category.ItemFactory] = 0,
		[Category.Light2D] = 0,
		[Category.PlayerList] = 0,
		[Category.PoolManager] = 0,
		[Category.SpriteManager] = 0,
		[Category.PlayerNetworkActions] = 0,
		[Category.SteamIntegration] = 0,
		[Category.TabUISystem] = 0,
		[Category.Telecommunications] = 0,
		[Category.PlayerJobManager] = 0,
		[Category.PushPull] = 0,
		[Category.NetSpriteImage] = 0,
		[Category.ShutterController] = 0,
		[Category.ShipNavigation] = 0,
		[Category.Lighting] = 0,
		[Category.Electrical] = 0,
		[Category.Unknown] = 0,
	};
	//Example of how to call Logger.Log("Message",Category.#Something#);
	private enum Level{
		Error = 0,
		Warning = 1,
		Log = 2,
		Trace = 3
		};

	public static void Log(string Message, Category Category = Category.Unknown,[CallerMemberName]string memberName = "")
		{
		Process(Message,Level.Log,Category,memberName);
		}
	public static void LogWarning(string Message, Category Category = Category.Unknown,[CallerMemberName]string memberName = "")
		{
		Process(Message,Level.Warning,Category,memberName);
		}

	public static void LogError(string Message, Category Category = Category.Unknown,[CallerMemberName]string memberName = "")
		{
		Process(Message,Level.Error,Category,memberName);
		}

	private static void Process(string Message,Level PriorityLevel, Category Category = Category.Unknown,string memberName = "")
		{
		int PriorityLevelint = (int)PriorityLevel;
		if (FilterDictionary.ContainsKey(Category)) 
			{
			if (!(FilterDictionary[Category] < PriorityLevelint)) 
				{
				Debug_Level_process((Category+" "+PriorityLevel.ToString()+" "+Message+"\n"+ memberName), PriorityLevelint);
				}
			}
		else
			{
			Debug_Level_process((Category+" "+PriorityLevel.ToString()+" "+Message+"\n"+ memberName), PriorityLevelint);
			}	
		}
	private static void Debug_Level_process(string Message,int PriorityLevel)
		{
		if (PriorityLevel == 0) 
			{
			Debug.LogError(Message);
			} 
		else if (PriorityLevel == 1)
			{
			Debug.LogWarning(Message);
			} 
		else 
			{
			Debug.Log(Message); 
			}
		}
	}


