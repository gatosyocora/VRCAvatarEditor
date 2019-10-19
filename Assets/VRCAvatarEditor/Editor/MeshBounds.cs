using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public static class MeshBounds
    {
        /// <summary>
        /// 特定のオブジェクト以下のメッシュのBoundsがすべて同じ範囲になるように設定する
        /// </summary>
        /// <param name="parentObj"></param>
        public static void BoundsSetter(GameObject parentObj, List<SkinnedMeshRenderer> exclusions, Vector3 boundsScale)
        {
            var renderers = parentObj.GetComponentsInChildren<SkinnedMeshRenderer>().ToList();

            Undo.RecordObjects(renderers.ToArray(), "Change Bounds");

            foreach (var renderer in renderers)
            {
                // 除外リストに含まれていれば処理しない
                if (exclusions.Contains(renderer)) continue;

                if (renderer == null) continue;

                var objScale = renderer.transform.localScale;
                var meshBoundsScale = new Vector3(boundsScale.x / objScale.x, boundsScale.y / objScale.y, boundsScale.z / objScale.z);
                renderer.localBounds = new Bounds(Vector3.zero, meshBoundsScale);
            }
        }

        [ExecuteInEditMode]
        private static void DrawBoundsArea(SkinnedMeshRenderer renderer)
        {
            var cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var cubeMesh = cubeObj.GetComponent<MeshFilter>().sharedMesh;

            var transform = renderer.transform;
            var bounds = renderer.bounds;

            Gizmos.DrawWireMesh(cubeMesh, 
                                transform.position + bounds.center,
                                transform.rotation);
        }
    }

}
