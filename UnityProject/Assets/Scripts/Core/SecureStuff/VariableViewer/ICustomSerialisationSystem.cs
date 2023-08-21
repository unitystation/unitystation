using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SecureStuff
{
	public interface ICustomSerialisationSystem
	{

		public bool CanDeSerialiseValue(Type InType);
		public object DeSerialiseValue(string StringData, Type InType);

	}
}

