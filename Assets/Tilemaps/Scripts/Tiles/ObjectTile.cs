﻿using System;
using Tilemaps.Scripts.Behaviours.Objects;
using Tilemaps.Scripts.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Tilemaps.Scripts.Tiles
{
    [Serializable]
    public class ObjectTile : LayerTile
    {
        public GameObject Object;
        public bool Rotatable;
        public bool KeepOrientation;
        public bool Offset;
        public bool IsItem { get; private set; }

        private GameObject _objectCurrent;

        private void OnValidate()
        {
            if (Object != null)
            {
                if (_objectCurrent == null)
                {
                    // if sprite already exists (e.g. at startup), then load it, otherwise create a new one
                    EditorApplication.delayCall += () => { PreviewSprite = PreviewSpriteBuilder.LoadSprite(Object) ?? PreviewSpriteBuilder.Create(Object); };
                }
                else if (Object != _objectCurrent)
                {
                    // from one object -> other (overwrite current sprite)
                    EditorApplication.delayCall += () => { PreviewSprite = PreviewSpriteBuilder.Create(Object); };
                }
            }
            else if (_objectCurrent != null)
            {
                // setting to None object (delete current sprite)
                var obj = _objectCurrent;
                EditorApplication.delayCall += () => { PreviewSpriteBuilder.DeleteSprite(obj); };
            }

            _objectCurrent = Object;

            if (_objectCurrent != null)
            {
                IsItem = _objectCurrent.GetComponentInChildren<RegisterItem>() != null;
            }
        }

        public void SpawnObject(Vector3Int position, Tilemap tilemap, Matrix4x4 transformMatrix)
        {
            if (!Object)
                return;

            var go = Instantiate(Object);
            go.SetActive(false);
            go.transform.parent = tilemap.transform;

            var offset = new Vector3(transformMatrix.m03, transformMatrix.m13, transformMatrix.m23);
            
            go.transform.localPosition = position + new Vector3(0.5f, 0.5f, 0) + offset;
            go.transform.rotation = tilemap.transform.rotation * transformMatrix.rotation;

            go.name = Object.name;

            var registerObject = go.GetComponent<RegisterObject>() ?? go.AddComponent<RegisterObject>();

            registerObject.Offset = Vector3Int.RoundToInt(-offset);

            go.SetActive(true);
        }

        public override Matrix4x4 Rotate(Matrix4x4 transformMatrix, bool clockwise)
        {
            if (Rotatable)
            {
                var rotation = Quaternion.Euler(0f, 0f, clockwise ? 90f : -90f);

                var newRotation = KeepOrientation ? Quaternion.identity : transformMatrix.rotation * rotation;
                var newTranslation = !Offset ? Vector3.zero : rotation * transformMatrix.GetColumn(3);

                if (Offset && Math.Abs(newTranslation.magnitude) < 0.1)
                {
                    newTranslation = Vector3.up;
                    newRotation = Quaternion.identity;
                }

                return Matrix4x4.TRS(newTranslation, newRotation, Vector3.one);
            }
            return base.Rotate(transformMatrix, clockwise);
        }
    }
}