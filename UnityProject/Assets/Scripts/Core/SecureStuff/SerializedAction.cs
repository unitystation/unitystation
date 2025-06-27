using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if true
using UnityEditor;
#endif

namespace SecureStuff
{
	[System.Serializable]
	public class SerializedAction
	{
		[SerializeField] private UnityEngine.Object target;
		[SerializeField] private string methodName;
		[SerializeField] private string parameterType;

		public Action CachedAction;
		public Action<Vector2> CachedVector2Action;
		public Action<Vector3> CachedVector3Action;
		public Action<Vector2Int> CachedVector2IntAction;
		public Action<Vector3Int> CachedVector3IntAction;

		public void Invoke()
		{
			if (CachedAction == null) Cache();
			CachedAction?.Invoke();
		}

		public void Invoke(Vector2 param)
		{
			if (CachedVector2Action == null) Cache<Vector2>();
			CachedVector2Action?.Invoke(param);
		}

		public void Invoke(Vector3 param)
		{
			if (CachedVector3Action == null) Cache<Vector3>();
			CachedVector3Action?.Invoke(param);
		}

		public void Invoke(Vector2Int param)
		{
			if (CachedVector2IntAction == null) Cache<Vector2Int>();
			CachedVector2IntAction?.Invoke(param);
		}

		public void Invoke(Vector3Int param)
		{
			if (CachedVector3IntAction == null) Cache<Vector3Int>();
			CachedVector3IntAction?.Invoke(param);
		}

		private void Cache()
		{
			if (target == null || string.IsNullOrEmpty(methodName))
				return;

			var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			if (method != null && method.GetParameters().Length == 0)
			{
				CachedAction += (Action)Delegate.CreateDelegate(typeof(Action), target, method);
				parameterType = "None";
			}
			else
			{
				Debug.LogWarning($"Method '{methodName}' on {target} not found or has parameters.");
			}
		}

		private void Cache<T>()
		{
			if (target == null || string.IsNullOrEmpty(methodName)) return;

			var method = target.GetType().GetMethod(methodName,
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				null, new[] { typeof(T) }, null);

			if (method != null)
			{
				var delegateType = typeof(Action<>).MakeGenericType(typeof(T));
				if (typeof(T) == typeof(Vector2))
					CachedVector2Action += (Action<Vector2>)Delegate.CreateDelegate(delegateType, target, method);
				else if (typeof(T) == typeof(Vector3))
					CachedVector3Action += (Action<Vector3>)Delegate.CreateDelegate(delegateType, target, method);
				else if (typeof(T) == typeof(Vector2Int))
					CachedVector2IntAction += (Action<Vector2Int>)Delegate.CreateDelegate(delegateType, target, method);
				else if (typeof(T) == typeof(Vector3Int))
					CachedVector3IntAction += (Action<Vector3Int>)Delegate.CreateDelegate(delegateType, target, method);

				parameterType = typeof(T).Name;
			}
		}
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SerializedAction))]
    public class SerializedActionDrawer : PropertyDrawer
    {
        private const BindingFlags methodFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var targetProp = property.FindPropertyRelative("target");
            var methodProp = property.FindPropertyRelative("methodName");

            Rect objectRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            Rect methodRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(objectRect, targetProp, new GUIContent(label.text + " Target"));

            UnityEngine.Object targetObj = targetProp.objectReferenceValue;
            List<string> methodNames = new List<string> { "<None>" };
            int selectedIndex = 0;

            if (targetObj != null)
            {
                MethodInfo[] methods = targetObj.GetType().GetMethods(methodFlags);
                foreach (var m in methods)
                {
                    if (m.ReturnType == typeof(void))
                    {
                        var parameters = m.GetParameters();
                        if (parameters.Length == 0 ||
                            (parameters.Length == 1 && (
                             parameters[0].ParameterType == typeof(Vector2) ||
                             parameters[0].ParameterType == typeof(Vector3) ||
                             parameters[0].ParameterType == typeof(Vector2Int) ||
                             parameters[0].ParameterType == typeof(Vector3Int))))
                        {
                            string methodSignature = GetMethodSignature(m);
                            methodNames.Add(methodSignature);
                            if (m.Name == methodProp.stringValue)
                                selectedIndex = methodNames.Count - 1;
                        }
                    }
                }
            }

            int newIndex = EditorGUI.Popup(methodRect, "Method", selectedIndex, methodNames.ToArray());
            if (newIndex != selectedIndex)
            {
                methodProp.stringValue = newIndex == 0 ? "" : ExtractMethodName(methodNames[newIndex]);
            }

            EditorGUI.EndProperty();
        }

        private string GetMethodSignature(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (parameters.Length == 0) return method.Name;

            string paramList = string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name}"));
            return $"{method.Name}({paramList})";
        }

        private string ExtractMethodName(string methodSignature)
        {
            return methodSignature.Split('(')[0];
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2 + 4;
        }
    }
#endif
}