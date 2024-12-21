using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NaughtyAttributes;
using SecureStuff;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

public class EventRouter : MonoBehaviour
{
	//TODO Specifying data sometime
	public List<EventConnection> EventLinks = new List<EventConnection>();

	// Start is called before the first frame update
	void Start()
	{
		PopulateEventRouter();
	}

	[Button]
	public void PopulateEventRouter()
	{
		foreach (var eventLink in EventLinks)
		{
			AllowedReflection.PopulateEventRouter(eventLink);
		}
	}
}

#if UNITY_EDITOR
[CustomEditor(typeof(EventRouter))]
public class EventRouterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EventRouter eventRouter = (EventRouter)target;

        if (eventRouter.EventLinks == null)
        {
            eventRouter.EventLinks = new List<EventConnection>();
        }

        for (int i = 0; i < eventRouter.EventLinks.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Event Connection {i + 1}", EditorStyles.boldLabel);
            eventRouter.EventLinks[i].TargetComponent = (MonoBehaviour)EditorGUILayout.ObjectField("Target Component", eventRouter.EventLinks[i].TargetComponent, typeof(MonoBehaviour), true);
            eventRouter.EventLinks[i].TargetFunction = EditorGUILayout.TextField("Target Function String", eventRouter.EventLinks[i].TargetFunction);
            if (eventRouter.EventLinks[i].TargetComponent != null)
            {
                var methods = eventRouter.EventLinks[i].TargetComponent.GetType()
                    .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => m.GetParameters().Length == 0 && m.ReturnType == typeof(void))
                    .Select(m => m.Name)
                    .ToArray();

                int selectedIndex = Array.IndexOf(methods, eventRouter.EventLinks[i].TargetFunction);
                selectedIndex = EditorGUILayout.Popup("Target Function", selectedIndex, methods);
                if (selectedIndex >= 0)
                {
                    eventRouter.EventLinks[i].TargetFunction = methods[selectedIndex];
                }
            }
            eventRouter.EventLinks[i].SourceComponent = (MonoBehaviour)EditorGUILayout.ObjectField("Source Component", eventRouter.EventLinks[i].SourceComponent, typeof(MonoBehaviour), true);
            eventRouter.EventLinks[i].SourceEvent = EditorGUILayout.TextField("Source Event String", eventRouter.EventLinks[i].SourceEvent);

            if (eventRouter.EventLinks[i].SourceComponent != null)
            {
                var methods = eventRouter.EventLinks[i].SourceComponent.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(m => (m.FieldType == typeof(Action) || m.FieldType == typeof(UnityEvent)))
                    .Select(m => m.Name)
                    .ToArray();

                int selectedIndex = Array.IndexOf(methods, eventRouter.EventLinks[i].SourceEvent);
                selectedIndex = EditorGUILayout.Popup("Source Event", selectedIndex, methods);
                if (selectedIndex >= 0)
                {
                    eventRouter.EventLinks[i].SourceEvent = methods[selectedIndex];
                }
            }

            if (GUILayout.Button("Remove Event Connection"))
            {
                eventRouter.EventLinks.RemoveAt(i);
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Event Connection"))
        {
            eventRouter.EventLinks.Add(new EventConnection());
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(eventRouter);
        }
    }
}
#endif