﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using UnityEngine;

public class AdminLogging : MonoBehaviour
{
	private static AdminLogging adminLogging;
	public static AdminLogging Instance
	{
		get
		{
			if (adminLogging == null)
			{
				adminLogging = FindObjectOfType<AdminLogging>();
			}

			return adminLogging;
		}
	}

	private string adminLogPath;
	private string adminLog2Path;
	private bool logswitcher = true;

	private void Awake()
	{
		AdminLogStart();
	}

	private void AdminLogStart()
	{
		adminLogPath = Path.Combine(Application.streamingAssetsPath, "Logging", "adminlog1.txt");
		adminLog2Path = Path.Combine(Application.streamingAssetsPath, "Logging", "adminlog2.txt");

		//if (!logswitcher)
		//{
		//	adminLogPath = adminLog2Path;

		//	File.CreateText(adminLog2Path).Close();

		//	logswitcher = true;
		//}
		//else
		//{
		//	File.CreateText(adminLogPath).Close();

		//	logswitcher = false;
		//}

		if (!File.Exists(adminLogPath))
		{
			//Creates file
			var file = File.CreateText(adminLogPath);

			//Todo add to file round number here
			file.WriteLine("true");

			file.Close();
		}

		if (!File.Exists(adminLog2Path))
		{
			//Creates file
			var file = File.CreateText(adminLog2Path);

			//Todo add to file round number here
			file.WriteLine("false");

			file.Close();
		}

		var first = File.ReadLines(adminLogPath).First() == "true";

		var second = File.ReadLines(adminLog2Path).First() == "true";

		if (first && !second)
		{
			//Clears file
			var file = File.CreateText(adminLogPath);

			//Todo add to file round number here
			file.WriteLine("false");

			file.Close();
		}
		else if (!first && second)
		{
			//Clears file
			var file = File.CreateText(adminLogPath);

			//Todo add to file round number here
			file.WriteLine("true");

			file.Close();
		}
		else if (first && second)
		{
			//Clears file
			var file = File.CreateText(adminLog2Path);

			//Todo add to file round number here
			file.WriteLine("false");

			file.Close();

			adminLogPath = adminLog2Path;
		}
		else
		{
			//Clears file
			var file = File.CreateText(adminLog2Path);

			//Todo add to file round number here
			file.WriteLine("true");

			file.Close();

			adminLogPath = adminLog2Path;
		}
	}

	/// <summary>
	/// Adds admin actions to the admin chat and to admin logging file
	/// </summary>
	/// <param name="msg">Message displayed</param>
	/// <param name="userId">Id of admin</param>
	/// <param name="txt">Extra Text needed, e.g text of command report</param>
	public void AddToAdminChatAndLog(string msg, string userId, string txt = null)
	{
		UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg, userId);

		if (txt != null)
		{
			txt = "\n" + txt;
		}

		msg = "Time " + DateTime.UtcNow.ToShortTimeString() + " : " + userId + " : " + msg + txt;

		File.AppendAllLines(adminLogPath, new string[]
		{
			"\r\n" + msg
		});
	}
}
