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
            RevertBoundsToPrefab(renderers);
            var avatarBounds = CalcAvatarBoundsSize(renderers);
            var offset = Vector3.zero;
            Transform boneTrans;

            Undo.RecordObjects(renderers.ToArray(), "Change Bounds");

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;

                boneTrans = renderer.rootBone.transform;
                var  scale = GetScale(boneTrans);

                var center = avatarBounds.center - boneTrans.position;
                center = Quaternion.Inverse(boneTrans.rotation) * center;
                center = new Vector3(center.x / scale.x,
                                        center.y / scale.y,
                                        center.z / scale.z);
                
                var size = Quaternion.Inverse(renderer.rootBone.transform.rotation) * avatarBounds.size;
                size = new Vector3( size.x / scale.x,
                                    size.y / scale.y,
                                    size.z / scale.z);
                renderer.localBounds = new Bounds(center, size);
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
            var offset = Vector3.zero;
            Bounds rendererBounds;

            foreach (var renderer in renderers)
            {
                avatarCenter += renderer.bounds.center;
            }
            avatarCenter /= renderers.Count;

            var avatarBounds = new Bounds(avatarCenter, Vector3.zero);
            foreach (var renderer in renderers)
            {
                rendererBounds = renderer.bounds;
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

        /// <summary>
        /// 特定のオブジェクトのScaleを取得
        /// </summary>
        /// <param name="transform"></param>
        /// <returns></returns>
        private static Vector3 GetScale(Transform transform)
        {
            var scale = Vector3.one;

            while(true)
            {
                scale.Scale(transform.localScale);
                transform = transform.parent;

                if (transform == null) break;
            }

            return scale;
        }
    }

}
