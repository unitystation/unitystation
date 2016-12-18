using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UI;

namespace Events { 

    public class UIEvent: UnityEvent<GameObject> {}

    public class EventManager : MonoBehaviour {

        private Dictionary<string, UIEvent> uiEvents = new Dictionary<string, UIEvent>();

        private static EventManager eventManager;

        public static EventManager Instance {
            get {
                if(!eventManager) {
                    eventManager = FindObjectOfType<EventManager>();
                }

                return eventManager;
            }
        }
	
	    public static void AddUIListener(string eventName, UnityAction<GameObject> listener) {
            if(eventName.Length == 0)
                return;
            
            UIEvent uiEvent;

            if(!Instance.uiEvents.TryGetValue(eventName, out uiEvent)) {
                uiEvent = new UIEvent();
                Instance.uiEvents[eventName] = uiEvent;
            }

            uiEvent.AddListener(listener);            
        }

        public static void RemoveUIListener(string eventName, UnityAction<GameObject> listener) {
            if(Instance.uiEvents.ContainsKey(eventName)) {
                Instance.uiEvents[eventName].RemoveListener(listener);
            }
        }

        public static void TriggerUIEvent(string eventName, GameObject item) {
            if(Instance.uiEvents.ContainsKey(eventName)) {
                Instance.uiEvents[eventName].Invoke(item);

            }
        }
    }
}
