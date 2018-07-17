using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public enum Categories{
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
	private static Dictionary<Categories, int> FilterDictionary = new Dictionary<Categories, int>{
		[Categories.Atmospherics] = 0,
		[Categories.Movement] = 0,
		[Categories.MapLoad] = 0,
		[Categories.RoomControl] = 0,
		[Categories.ItemList] = 0,
		[Categories.ItemEntry] = 0,
		[Categories.DmiIconData] = 0,
		[Categories.UI] = 0,
		[Categories.MatrixManager] = 0,
		[Categories.Equipment] = 0,
		[Categories.NetworkManager] = 0,
		[Categories.uniCloth] = 0,
		[Categories.NetworkTabManager] = 0,
		[Categories.ItemFactory] = 0,
		[Categories.Light2D] = 0,
		[Categories.PlayerList] = 0,
		[Categories.PoolManager] = 0,
		[Categories.SpriteManager] = 0,
		[Categories.PlayerNetworkActions] = 0,
		[Categories.SteamIntegration] = 0,
		[Categories.TabUISystem] = 0,
		[Categories.Telecommunications] = 0,
		[Categories.PlayerJobManager] = 0,
		[Categories.PushPull] = 0,
		[Categories.NetSpriteImage] = 0,
		[Categories.ShutterController] = 0,
		[Categories.ShipNavigation] = 0,
		[Categories.Lighting] = 0,
		[Categories.Electrical] = 0,
		[Categories.Unknown] = 0,
	};
	//Example of how to call TADB_Debug.Log("Message",Categories.#Something#.ToString());
	private enum Levels{
		Error = 0,
		Warning = 1,
		Log = 2,
		Trace = 3
		};

	private static List<string> PriorityMessage = new List<string>{
		"Error ",
		"Warning ",
		"Log ",
		"Trace ",
	};

	public static void Log(string Message, Categories Category = Categories.Unknown,[CallerMemberName]string memberName = "")
		{
		Process(Message,Levels.Log,Category,memberName);
		}
	public static void LogWarning(string Message, Categories Category = Categories.Unknown,[CallerMemberName]string memberName = "")
		{
		Process(Message,Levels.Warning,Category,memberName);
		}

	public static void LogError(string Message, Categories Category = Categories.Unknown,[CallerMemberName]string memberName = "")
		{
		Process(Message,Levels.Error,Category,memberName);
		}

	private static void Process(string Message,Levels PriorityLevel, Categories Category = Categories.Unknown,string memberName = "")
		{
		int PriorityLevelint = (int)PriorityLevel;
		if (FilterDictionary.ContainsKey(Category)) 
			{
			if (!(FilterDictionary[Category] < PriorityLevelint)) 
				{
				Debug_Level_process((Category+" "+PriorityMessage[PriorityLevelint] +Message+"\n"+ memberName), PriorityLevelint);
				}
			}
		else
			{
			Debug_Level_process((Category+" "+PriorityMessage[PriorityLevelint]+Message+"\n"+ memberName), PriorityLevelint);
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


