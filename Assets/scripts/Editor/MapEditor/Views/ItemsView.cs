using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapEditor {
    
    public class ItemsView: CategoryView {

        public ItemsView() : base("Items", "Category") {
            optionList.Add("Kitchen", "Items/Kitchen", "Items");
        }

        protected override void DrawContent() {
        }
    }
}