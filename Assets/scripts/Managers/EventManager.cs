using UnityEngine.Events;
using UnityEngine;

namespace Events {

    public class UIEvent: UnityEvent<GameObject> { }

    [ExecuteInEditMode]
    public class EventManager : MonoBehaviour {

        private EventController<string, GameObject> ui = new EventController<string, GameObject>();

        public static EventController<string, GameObject> UI {
            get { return Instance.ui; }
        }

        private static EventManager eventManager;

        public static EventManager Instance {
            get {
                if(!eventManager) {
                    eventManager = FindObjectOfType<EventManager>();
                }
                return eventManager;
            }
        }

		public static void UpdateLights(){
			
		}
    }
}
