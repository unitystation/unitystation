using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapEditor {
    
    public class ItemsView: CategoryView {

        public ItemsView(IEnumerable<string> fileName, string path) : base("Items", "Category") {
            string pathString = path + "/Prefabs/Items";
            foreach (string f in fileName)
            {
                if ((f).Contains(pathString) && (f != pathString))
                {
                    string stringName = f.Replace(path + "/Prefabs/", "");
                    optionList.Add(stringName, stringName, stringName);
                }
            }
            //optionList.Add("Kitchen", "Items/Kitchen", "Items");
        }

        protected override void DrawContent() {
        }
    }
}