using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MapEditor
{

    public class StructuresView : CategoryView
    {

        public StructuresView(IEnumerable<string> fileName, string path) : base("Structures", "Category")
        {
            string pathString = path + "/Prefabs/Structures";
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
        }
        private int i;
        protected override void DrawContent()
        {

        }
    }
}