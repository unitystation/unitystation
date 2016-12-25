using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using UI;

namespace Events {

    public class UIEvent: UnityEvent<GameObject> { }
    public class MatrixEvent: UnityEvent<TileType> { }

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

        private static int hashCode(Vector3 vector) {
            int x = vector.x.GetHashCode();
            int y = vector.y.GetHashCode();

            int hash = 17;
            hash = ((hash + x) << 5) - (hash + x);
            hash = ((hash + y) << 5) - (hash + y);
            return hash;
        }
    }

}
