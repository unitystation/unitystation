using UnityEngine;

namespace Util
{
	public class ArrayElementTitleAttribute : PropertyAttribute
	{
		public string Varname;
		public string Nullname;
		public ArrayElementTitleAttribute(string ElementTitleVar, string NullvalueString = "null")
		{
			Varname = ElementTitleVar;
			Nullname = NullvalueString;
		}
	}
}
