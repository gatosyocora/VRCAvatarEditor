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
        public static void BoundsSetter(List<SkinnedMeshRenderer> renderers)
        {
            var avatarBounds = CalcAvatarBoundsSize(renderers);

            Debug.Log("center:"+avatarBounds.center+", size:"+avatarBounds.size);

            Undo.RecordObjects(renderers.ToArray(), "Change Bounds");

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.localBounds = avatarBounds;
            }
        }

        // TODO: rootBoneが異なるものを持つアバターだとうまく計算できない
        /// <summary>
        /// アバター全体を囲うBoundsのサイズを計算する
        /// </summary>
        /// <param name="renderers"></param>
        /// <returns></returns>
        private static Bounds CalcAvatarBoundsSize(List<SkinnedMeshRenderer> renderers)
        {
            var avatarCenter = Vector3.zero;
            var avatarMin = Vector3.zero;
            var avatarMax = Vector3.zero;
            Bounds rendererBounds;

            foreach (var renderer in renderers)
            {
                avatarCenter += renderer.localBounds.center;
            }
            avatarCenter /= renderers.Count;

            var avatarBounds = new Bounds(avatarCenter, Vector3.zero);
            foreach (var renderer in renderers)
            {
                rendererBounds = renderer.localBounds;
                avatarBounds.Encapsulate(rendererBounds.min);
                avatarBounds.Encapsulate(rendererBounds.max);
            }
            return avatarBounds;
        }

        /// <summary>
        /// exclusionsに含まれるものを除いたrootObject以下のSkinnedMeshRendererをすべて取得する
        /// </summary>
        /// <param name="rootObject"></param>
        /// <param name="exclusions"></param>
        /// <returns></returns>
        public static List<SkinnedMeshRenderer> GetSkinnedMeshRenderersWithoutExclusions(GameObject rootObject, List<SkinnedMeshRenderer> exclusions)
        {
            return rootObject
                    .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                    .Where(x => !exclusions.Contains(x))
                    .ToList();
        }

        /// <summary>
        /// BoundsサイズをSceneViewに表示する
        /// </summary>
        /// <param name="renderer"></param>
        public static void DrawBoundsGizmo(SkinnedMeshRenderer renderer)
        {
            var bounds = renderer.bounds;
            Handles.color = Color.white;
            Handles.DrawWireCube(bounds.center, bounds.size);
        }

        /// <summary>
        /// Boundsの値をPrefabの状態に戻す
        /// </summary>
        /// <param name="renderers"></param>
        public static void RevertBoundsToPrefab(List<SkinnedMeshRenderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                PrefabUtility.ReconnectToLastPrefab(renderer.gameObject);

                var so = new SerializedObject(renderer);
                so.Update();

                var sp = so.FindProperty("m_AABB");
#if UNITY_2018_3_OR_NEWER
                var currentEnabled = renderer.enabled;
                // Transform has 'ReflectionProbeAnchorManager::kChangeSystem' change interests present when destroying the hierarchy.
                // 対策で一度disableにする
                renderer.enabled = false;
                PrefabUtility.RevertPropertyOverride(sp, InteractionMode.UserAction);
                renderer.enabled = currentEnabled;
#else
                sp.prefabOverride = false;
                sp.serializedObject.ApplyModifiedProperties();
#endif
            }
        }
    }

}
