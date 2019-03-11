using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class MeshBounds
    {

        /// <summary>
        /// 特定のオブジェクト以下のメッシュのBoundsがすべて同じ範囲になるように設定する
        /// </summary>
        /// <param name="parentObj"></param>
        public static void BoundsSetter(GameObject parentObj, List<GameObject> exclusions, Vector3 boundsScale)
        {
            var objs = GetAllChildrens(parentObj);

            foreach (var obj in objs)
            {
                // 除外リストに含まれていれば処理しない
                if (exclusions.Contains(obj)) continue;

                var mesh = obj.GetComponent<MeshRenderer>();
                var skinnedMesh = obj.GetComponent<SkinnedMeshRenderer>();

                if (mesh == null && skinnedMesh == null) continue;

                // Mesh Rendererの場合
                if (mesh != null)
                {
                    Undo.RecordObject(mesh, "Change Transform " + mesh.name);
                }
                // SkinnedMeshRendererの場合
                else
                {
                    Undo.RecordObject(skinnedMesh, "Change Transform " + skinnedMesh.name);

                    var objScale = skinnedMesh.gameObject.transform.localScale;
                    var meshBoundsScale = new Vector3(boundsScale.x / objScale.x, boundsScale.y / objScale.y, boundsScale.z / objScale.z);
                    skinnedMesh.localBounds = new Bounds(Vector3.zero, meshBoundsScale);
                }

            }

        }

        /// <summary>
        /// 指定オブジェクトの子オブジェクト以降をすべて取得する
        /// </summary>
        /// <param name="parentObj"></param>
        /// <returns></returns>
        private static List<GameObject> GetAllChildrens(GameObject parentObj)
        {
            List<GameObject> objs = new List<GameObject>();

            var childTransform = parentObj.GetComponentsInChildren<Transform>();

            foreach (Transform child in childTransform)
            {
                objs.Add(child.gameObject);
            }

            return objs;
        }
    }

}
