using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace MapEditor
{

    public class ItemsView : CategoryView
    {

        public ItemsView(IEnumerable<string> fileName, string path) : base("Items", "Category")
        {
            string pathString = path + "/Prefabs/Items";
            foreach (string f in fileName)
            {
                if ((f).Contains(pathString) && (f != pathString))
                {
                    //name, path, subsection
                    string stringName = f.Replace(path + "/Prefabs/", "");
                    string subsection = stringName.Substring(stringName.LastIndexOf("/") + 1);
                    optionList.Add(subsection, stringName, subsection);

                }
            }
            //optionList.Add("Kitchen", "Items/Kitchen", "Items");
        }

        protected override void DrawContent()
        {
        }
    }
}