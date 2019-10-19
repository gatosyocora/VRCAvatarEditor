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
        public static void BoundsSetter(List<SkinnedMeshRenderer> renderers, Vector3 boundsScale)
        {
            Undo.RecordObjects(renderers.ToArray(), "Change Bounds");

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                var objScale = renderer.transform.localScale;
                var meshBoundsScale = new Vector3(boundsScale.x / objScale.x, boundsScale.y / objScale.y, boundsScale.z / objScale.z);
                renderer.localBounds = new Bounds(Vector3.zero, meshBoundsScale);
            }
        }

        /// <summary>
        /// アバター全体を囲うBoundsのサイズを計算する
        /// </summary>
        /// <param name="renderers"></param>
        /// <returns></returns>
        /// 
        /*
        private static Vector3 CalcAvatarBoundsSize(List<SkinnedMeshRenderer> renderers)
        {
            Bounds avatarBounds = new Bounds(Vector3.zero, Vector3.zero);

            foreach (var renderer in renderers)
            {
                var bounds = renderer.bounds;

                avatarBounds.Contains(bounds.min)
            }
        }
        */

        public static List<SkinnedMeshRenderer> GetSkinnedMeshRenderersWithoutExclusions(GameObject rootObject, List<SkinnedMeshRenderer> exclusions)
        {
            return rootObject
                    .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(x => !exclusions.Contains(x))
                    .ToList();
        }

        public static void DrawBoundsGizmo(SkinnedMeshRenderer renderer)
        {
            var bounds = renderer.bounds;
            Handles.color = Color.white;
            Handles.DrawWireCube(bounds.center, bounds.size);
        }
    }

}
