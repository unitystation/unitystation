using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace MapEditor
{

    public class ObjectsView : CategoryView
    {

        public ObjectsView(IEnumerable<string> fileName, string path) : base("Objects", "Section")
        {
            string pathString = path + "/Prefabs/Objects";
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
            /*
             optionList.Add("Tables", "Tables", "Tables");
             optionList.Add("Wall Mounts", "WallMounts", "Objects");
             optionList.Add("Lighting", "Lighting", "Objects");
             optionList.Add("Disposals", "Disposals", "Objects");
             optionList.Add("Atmos", "Atmos", "Objects");
             optionList.Add("Kitchen", "Machines/Kitchen", "Objects*/
        }

        protected override void DrawContent()
        {
        }
    }
}