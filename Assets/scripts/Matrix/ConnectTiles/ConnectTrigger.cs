using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Matrix {

    [ExecuteInEditMode]
    public class ConnectTrigger: MonoBehaviour {

        [HideInInspector]
        public int connectTypeIndex;
        private int currentConnectTypeIndex;
        public ConnectType ConnectType { get; private set; }
        
        void Start() {
            currentConnectTypeIndex = connectTypeIndex;
            UpdateConnectType(ConnectType.List[currentConnectTypeIndex]);

            UpdatePosition();
        }

        void OnValidate() {
            if(currentConnectTypeIndex != connectTypeIndex) {
                currentConnectTypeIndex = connectTypeIndex;
                UpdateConnectType(ConnectType.List[currentConnectTypeIndex]);
            }
        }

        public void UpdatePosition() {
            int x = Mathf.RoundToInt(transform.position.x);
            int y = Mathf.RoundToInt(transform.position.y);

            foreach(var c in GetComponentsInChildren<TileConnect>()) {
                c.UpdatePosition(x, y);
            }
        }

        private void UpdateConnectType(ConnectType connectType) {
            ConnectType = connectType;

            GetComponent<RegisterTile>().UpdatePosition();
        }
    }
}