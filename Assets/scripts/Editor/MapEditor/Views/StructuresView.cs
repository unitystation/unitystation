using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace MapEditor {

    public class StructuresView: CategoryView {

        public StructuresView() : base("Structures", "Category") {
            optionList.Add("Walls", "Walls", "Walls");
            optionList.Add("Floors", "Floors", "Floors");
            optionList.Add("Doors", "Doors", "Doors");
            optionList.Add("Windows", "Windows", "Windows");
        }

        private int i;

        protected override void DrawContent() {
            
        }
    }
}