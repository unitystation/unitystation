using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MapEditor {

    public class InputControl {
        private static bool keyDown = false;
        private static bool mouseDown = false;
        private static int oldID;

        public static bool AllowMouse { get; set; }
        public static int ControlIDHashCode { get; set; }

        //to handle different keyboard types (i.e. German and US);
        private static bool _rotateOptA;
        private static bool _rotateOptB;

        public static bool RotateOptA {
            get { return _rotateOptA; }
            set {
                _rotateOptA = value;
                if(!_rotateOptA && !_rotateOptB) {
                    RotateOptB = true;
                }
                if(_rotateOptA && _rotateOptB) {
                    RotateOptB = false;
                }
            }
        }

        public static bool RotateOptB {
            get { return _rotateOptB; }
            set {
                _rotateOptB = value;
                if(!_rotateOptA && !_rotateOptB) {
                    RotateOptA = true;
                }
                if(_rotateOptB && _rotateOptA) {
                    RotateOptA = false;
                }
            }
        }

        public static void ReactInput(SceneView sceneView) {
            if(AllowMouse) {
                CheckMouseControls(Event.current);
            }
            CheckKeyControls(Event.current);
        }

        private static void CheckMouseControls(Event e) {
            if(!e.isMouse)
                return;

            int controlID = GUIUtility.GetControlID(ControlIDHashCode, FocusType.Passive);

            switch(e.GetTypeForControl(controlID)) {
                case EventType.MouseDown:
                    if(e.button == 0) {
                        oldID = GUIUtility.hotControl;
                        GUIUtility.hotControl = controlID;
                        mouseDown = true;
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if(mouseDown && e.button == 0) {
                        if(Selection.activeGameObject != null) {
                            SelectObject(e);
                            GUIUtility.hotControl = oldID;
                        } else {
                            if(!BuildControl.Build(e)) {
                                SelectObject(e);
                            }
                            GUIUtility.hotControl = 0;
                        }
                        mouseDown = false;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if(mouseDown) {
                        GUIUtility.hotControl = oldID;
                        oldID = 0;
                        e.Use();
                    }
                    break;
            }
            PreviewObject.ShowPreview = GUIUtility.hotControl == 0 || !mouseDown;
        }

        private static void CheckKeyControls(Event e) {
            if(e.isKey) {
                if(!keyDown && e.type == EventType.KeyDown) {
                    keyDown = true;

                    if(RotateKeys(e)) {
                        e.Use();
                    } else {
                        switch(e.character) {
                            case 'a':
                                BuildControl.Build(e);
                                e.Use();
                                break;
                            case 'd':
                                foreach(GameObject obj in Selection.gameObjects)
                                    Undo.DestroyObjectImmediate(obj);
                                e.Use();
                                break;
                            default:
                                keyDown = false;
                                break;
                        }
                    }
                } else if(e.type == EventType.KeyUp) {
                    keyDown = false;
                }
            }
        }

        //to handle different keyboard types (i.e. German and US);
        private static bool RotateKeys(Event e) {
            if(_rotateOptA) {
                if(e.character == 'z') {
                    PreviewObject.RotateBackwards();
                    return true;
                } else if(e.character == 'x') {
                    PreviewObject.RotateForwards();
                    return true;
                }
            } else {
                if(e.character == '>') {
                    PreviewObject.RotateBackwards();
                    return true;
                } else if(e.character == '<') {
                    PreviewObject.RotateForwards();
                    return true;
                }
            }
            return false;
        }

        private static void SelectObject(Event e) {
            var mousePosition = Camera.current.ScreenToWorldPoint(new Vector3(e.mousePosition.x, -e.mousePosition.y + Camera.current.pixelHeight));
            var collider = Physics2D.OverlapPoint(mousePosition);
            GameObject gameObject = null;
            if(collider)
                gameObject = Physics2D.OverlapPoint(mousePosition).gameObject;

            Selection.activeGameObject = gameObject;
        }
    }
}
