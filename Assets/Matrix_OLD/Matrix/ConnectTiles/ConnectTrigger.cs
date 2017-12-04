using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MatrixOld
{

    [ExecuteInEditMode]
    public class ConnectTrigger : MonoBehaviour
    {

        [HideInInspector]
        public int connectTypeIndex;
        private int currentConnectTypeIndex;
        public ConnectType ConnectType { get { return ConnectType.List[connectTypeIndex]; } }

        void Awake()
        {
            currentConnectTypeIndex = connectTypeIndex;
        }

        void Start()
        {
            UpdateConnectType();
            UpdatePosition();
        }

        void OnValidate()
        {
            if (currentConnectTypeIndex != connectTypeIndex)
            {
                currentConnectTypeIndex = connectTypeIndex;
                UpdateConnectType();
            }
        }

        public void UpdatePosition()
        {
            int x = Mathf.RoundToInt(transform.position.x);
            int y = Mathf.RoundToInt(transform.position.y);

            foreach (var c in GetComponentsInChildren<TileConnect>())
            {
                c.UpdatePosition(x, y);
            }
        }

        private void UpdateConnectType()
        {
            //            GetComponent<RegisterTile>().UpdateTile();
        }
    }
}