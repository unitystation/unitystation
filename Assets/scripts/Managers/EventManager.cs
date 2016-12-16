using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UI;

namespace Events { 

    public class UIEvent: UnityEvent<GameObject> {}

    public class EventManager : MonoBehaviour {

        private Dictionary<string, UIEvent> uiEvents = new Dictionary<string, UIEvent>();

        private static EventManager instance;

	    
	    void Awake () {
            // There can only be one
            if(!instance) {
                instance = this;
            }else {
                Destroy(this);
            }
		
	    }
	
	    public static void AddUIListener(string eventName, UnityAction<GameObject> listener) {
            if(eventName.Length == 0)
                return;

            UIEvent uiEvent;

            if(!instance.uiEvents.TryGetValue(eventName, out uiEvent)) {
                uiEvent = new UIEvent();
                instance.uiEvents[eventName] = uiEvent;
            }

            uiEvent.AddListener(listener);            
        }

        public static void RemoveUIListener(string eventName, UnityAction<GameObject> listener) {
            if(instance.uiEvents.ContainsKey(eventName)) {
                instance.uiEvents[eventName].RemoveListener(listener);
            }
        }

        public static void TriggerUIEvent(string eventName, GameObject item) {
            if(instance.uiEvents.ContainsKey(eventName)) {
                instance.uiEvents[eventName].Invoke(item);

            }
        }
    }
}
