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
		public string Serialise(object InObject, Type TypeOf);

		public object GetDefaultValue(Type InType);
		//Security review
		//has access to Activator.CreateInstance()
		//Currently secure stuff has aaccess to stuff with IAllowedReflection iimplemented in class
		//and
		//	bool IsClass = ListType.IsValueType == false
		// && (ListType == typeof(string)) == false
		// && ListType.IsGenericType == false
		// && ListType.IsDefined(typeof(System.SerializableAttribute))
		// or It's value type
		//so, Basically value type or has System.SerializableAttribute
		//Dictionary is the same
		//So this is all accessible with trusted mode off
		//Should it have free access if trusted mode is on?
		//VV  when would you need to make a class that doesn't have system.serialisable Implemented?
		//hummmmmmmm nah, I can't think of anything, If you're reading this in the future And you need it, lol.
		//Also this can't directly implement it since its in insecure land, So it will call then if it returns Null try the activator, With the  SerializableAttribute Check

	}
}

