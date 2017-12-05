using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MapEditor {
    public abstract class AbstractView {
        public abstract void OnGUI();
    }

    public abstract class CategoryView: AbstractView {
        public readonly string Name;
        private readonly string prefixLabel;

        private int popupIndex;

        protected OptionList optionList = new OptionList();

        protected CategoryView(string name, string prefixLabel) {
            Name = name;
            this.prefixLabel = prefixLabel;
        }

        public override void OnGUI() {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.PrefixLabel(prefixLabel);
            
            popupIndex = EditorGUILayout.Popup(popupIndex, optionList.Names);

            EditorGUILayout.EndHorizontal();

            if(optionList[popupIndex] != null)
                optionList[popupIndex].OnGUI();

            DrawContent();
        }

        protected abstract void DrawContent();
    }

    public class OptionList {
        private List<CategoryOption> options = new List<CategoryOption>();
        private List<string> names = new List<string>();

        public string[] Names {
            get {
                if(names.Count == 0)
                    return new string[] { "None" };
                return names.ToArray();
            }
        }

        public void Add(string name, string prefabPath, string subsectionPath) {
            Add(new CategoryOption(name, prefabPath, subsectionPath));
        }

        public void Add(CategoryOption option) {
            options.Add(option);
            UpdateNames();
        }

        public void Remove(CategoryOption option) {
            options.Remove(option);
            UpdateNames();
        }

        private void UpdateNames() {
            names.Clear();

            foreach(var option in options) {
                names.Add(option.Name);
            }
        }

        public CategoryOption this[int index] {
            get {
                if(options.Count == 0)
                    return null;
                return options[index];
            }
        }
    }

    public class CategoryOption: AbstractView {
        public readonly string Name;
        private Vector2 scrollPosition;
        private GridView gridView;

        public CategoryOption(string name, string prefabPath, string subsectionPath) {
            Name = name;

            gridView = new GridView(prefabPath, subsectionPath);
        }

        public override void OnGUI() {
            gridView.OnGUI();
        }
    }
}
