using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapEditor {

    public class ObjectsView: CategoryView {

        public ObjectsView() : base("Objects", "Section") {
            optionList.Add("Tables", "Tables", "Tables");
            optionList.Add("Wall Mounts", "WallMounts", "Objects");
            optionList.Add("Lighting", "Lighting", "Objects");
            optionList.Add("Disposals", "Disposals", "Objects");
            optionList.Add("Kitchen", "Machines/Kitchen", "Objects");
        }

        protected override void DrawContent() {
        }
    }
}