using Google.Protobuf.WellKnownTypes;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor.Tools
{
	/// <summary>
	/// Tool that allows to find all game object that has a specific property with null value.
	/// </summary>
	/// <remarks>
	/// Please keep in mind that this tool uses reflection and might take a very long time to run.
	/// </remarks>
	public class FindEmptyProperty : EditorWindow
	{
		private string propertyTypeToFind;

		[MenuItem("Tools/Find and Set Component")]
		private static void Init()
		{
			// Get existing open window or if none, make a new one:
			FindEmptyProperty window = (FindEmptyProperty)GetWindow(typeof(FindEmptyProperty));
			window.titleContent.text = "Find and Set Component";
			window.Show();
		}

		private void Find()
		{
			Object[] objects = Resources.FindObjectsOfTypeAll(typeof(Object));

			System.Type type = System.Type.GetType(propertyTypeToFind);

			List<Object> objs = new List<Object>();

			foreach (Object obj in objects)
			{
				if (obj.name == "Microwave")
				{
					var aaaaaa = 1;
				}
				MemberInfo[] members = obj.GetType().GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

				var a = members.Where(p => p.MemberType == MemberTypes.All);

				if (a.Count() > 0)
					objs.Add(obj);
			}
		}

		void OnGUI()
		{
			EditorGUILayout.HelpBox("Choose the name of the type you want to find\n\nYou can use the syntax described in .NET Type.GetType(string) documentation\n\nEx.: AddressableReferences.AddressableAudioSource,Assets for the type AddressableAudioSource, in namespace AddressableReferences in assembly Assets.", MessageType.Info); ;
			propertyTypeToFind = EditorGUILayout.TextField(propertyTypeToFind);

			EditorGUILayout.Separator();

			if (GUILayout.Button("Find"))
			{
				bool isOk = true;
				if (propertyTypeToFind == null)
				{
					EditorUtility.DisplayDialog("Error", "You must choose a type name to find", "OK");
					isOk = false;
				}

				System.Type type  = System.Type.GetType(propertyTypeToFind);
				if (type == null)
				{
					EditorUtility.DisplayDialog("Error", "Unknown type, try fully qualifying it with it's namespace and assembly (with a comma after the type name).", "OK");
					isOk = false;
				}

				if (isOk)
				{
					Find();
					EditorUtility.DisplayDialog("Complete", "It's done!", "OK");
				}
			}

		}
	}
}
