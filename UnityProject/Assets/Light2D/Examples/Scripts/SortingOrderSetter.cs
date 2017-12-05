using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Light2D.Examples
{
    [ExecuteInEditMode]
    public class SortingOrderSetter : MonoBehaviour
    {
        public int SortingOrder;

        private void Awake()
        {
            Set();
        }

        private void OnEnable()
        {
            Set();
        }

        private void Start()
        {
            Set();
        }

        public void Set()
        {
            foreach (var rend in GetComponentsInChildren<Renderer>())
            {
                rend.sortingOrder = SortingOrder;
            }
        }
    }
}