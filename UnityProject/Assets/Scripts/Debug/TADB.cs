using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;


public class TADB_Debug
	{
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
	};
	private static Dictionary<string, int> NameDictionary = new Dictionary<string, int>{
		["Atmospherics"] = 0,
		["Movement"] = 0,
		["MapLoad"] = 0,
		["RoomControl"] = 0,
		["ItemList"] = 0,
		["ItemEntry"] = 0,
		["DmiIconData"] = 0,
		["UI"] = 0,
		["MatrixManager"] = 0,
		["Equipment"] = 0,
		["NetworkManager"] = 0,
		["uniCloth"] = 0,
		["NetworkTabManager"] = 0,
		["ItemFactory"] = 0,
		["Light2D"] = 0,
		["PlayerList"] = 0,
		["PoolManager"] = 0,
		["SpriteManager"] = 0,
		["PlayerNetworkActions"] = 0,
		["SteamIntegration"] = 0,
		["TabUISystem"] = 0,
		["Telecommunications"] = 0,
		["PlayerJobManager"] = 0,
		["PushPull"] = 0,
		["NetSpriteImage"] = 0,
		["ShutterController"] = 0,
		["ShipNavigation"] = 0,
		["Lighting"] = 0,
		["Electrical"] = 0,
	};

	private static Dictionary<int, string> LevelMessage = new Dictionary<int, string>{
		[0]= "Error ",
		[1]= "Warning ",
		[2]= "Log "
			
		};
	public static void Log(string Message, string Category = "Unknown ",[CallerMemberName]string memberName = "")
		{
		Process(Message,2,Category,memberName);
		}
	public static void LogWarning(string Message, string Category = "Unknown ",[CallerMemberName]string memberName = "")
		{
		Process(Message,1,Category,memberName);
		}

	public static void LogError(string Message, string Category = "Unknown ",[CallerMemberName]string memberName = "")
		{
		Process(Message,0,Category,memberName);
		}

	private static void Process(string Message,int PriorityLevel, string Category = "Unknown ",string memberName = "")
		{
		if (NameDictionary.ContainsKey(Category)) 
			{
			if (!(NameDictionary[Category] < PriorityLevel)) 
				{
				Debug_Level_process((Category+" "+LevelMessage[PriorityLevel]+Message+"\n"+ memberName), PriorityLevel);
				}
			}
		else
			{
			Debug_Level_process((Category+" "+LevelMessage[PriorityLevel]+Message+"\n"+ memberName), PriorityLevel);
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


