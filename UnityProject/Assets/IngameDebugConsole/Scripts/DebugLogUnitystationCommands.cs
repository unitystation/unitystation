using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace IngameDebugConsole
{
	public class DebugLogUnitystationCommands 
	{
		[ConsoleMethod("suicide", "kill yo' self")]
		public static void RunSuicide()
		{
			SuicideMessage.Send(null);
		}
	}
}
