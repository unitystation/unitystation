using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SecureStuff
{
	public static class AllowedEnvironmentVariables
	{
		public static string GetTEST_SERVER()
		{
			return Environment.GetEnvironmentVariable("TEST_SERVER");
		}

		public static void SetMONO_REFLECTION_SERIALIZER()
		{
			Environment.SetEnvironmentVariable("MONO_REFLECTION_SERIALIZER", "yes");
		}
	}

}

