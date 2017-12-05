using UnityEngine.Events;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Events
{

    public class UIEvent : UnityEvent<GameObject> { }

    //For simple broadcasts:
    public enum EVENT { UpdateFov }; // + other events. Add them as you need them

    [ExecuteInEditMode]
    public class EventManager : MonoBehaviour
    {

        private EventController<string, GameObject> ui = new EventController<string, GameObject>();

        // Stores the delegates that get called when an event is fired (Simple Events)
        private static Dictionary<EVENT, Action> eventTable
        = new Dictionary<EVENT, Action>();

        public static EventController<string, GameObject> UI
        {
            get { return Instance.ui; }
        }

        private static EventManager eventManager;

        public static EventManager Instance
        {
            get
            {
                if (!eventManager)
                {
                    eventManager = FindObjectOfType<EventManager>();
                }
                return eventManager;
            }
        }

        public static void UpdateLights()
        {

        }

        /*
		 * Below is for the simple event handlers and broast methods:
		 */

        // Adds a delegate to get called for a specific event
        public static void AddHandler(EVENT evnt, Action action)
        {
            if (!eventTable.ContainsKey(evnt)) eventTable[evnt] = action;
            else eventTable[evnt] += action;
        }

        public static void RemoveHandler(EVENT evnt, Action action)
        {
            if (eventTable[evnt] != null)
                eventTable[evnt] -= action;
            if (eventTable[evnt] == null)
                eventTable.Remove(evnt);
        }

        // Fires the event
        public static void Broadcast(EVENT evnt)
        {
            if (eventTable.ContainsKey(evnt) && eventTable[evnt] != null) eventTable[evnt]();
        }
    }
}
