using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRCAvatarEditor.Utilities
{
    public class VRCAvatarMeshUtility
    {
        public const int LIPSYNC_SHYPEKEY_NUM = 15;

        public static string[] lipSyncBlendShapeNamePrefixPatterns =
        {
            "vrc.v_",
            "VRC.v_",
            "vrc_v_", // Shaon
        };

        public static SkinnedMeshRenderer GetFaceMeshRenderer(IVRCAvatarBase avatar)
        {
            var rootTransform = avatar.Animator.transform;

            // 直下のBodyという名前のメッシュがFaceMeshであることが多い
            var bodyMeshTransform = rootTransform.Find("Body");
            if (bodyMeshTransform != null)
            {
                var bodyMeshRenderer = bodyMeshTransform.GetComponent<SkinnedMeshRenderer>();
                if (bodyMeshRenderer != null && IsFaceMesh(bodyMeshRenderer.sharedMesh))
                {
                    return bodyMeshRenderer;
                }
            }

            // 全走査する
            var renderers = rootTransform.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (IsFaceMesh(renderer.sharedMesh))
                {
                    return renderer;
                }
            }

            return null;
        }

        public static IEnumerable<ExclusionBlendShape> GetExclusionBlendShapes(IVRCAvatarBase avatar, IEnumerable<string> blendshapeExclusions)
        {
            var exclusions = blendshapeExclusions.Select(n => new ExclusionBlendShape(n, ExclusionMatchType.Contain));

            if (avatar.LipSyncShapeKeyNames != null)
            {
                exclusions = exclusions
                                .Union(
                                    avatar.LipSyncShapeKeyNames
                                        .Select(n => new ExclusionBlendShape(n, ExclusionMatchType.Perfect))
                                );
            }
            return exclusions;
        }

        // TODO : モデルによっては前髪あたりまでviewpositionがいってしまう
        public static Vector3 CalcAvatarViewPosition(IVRCAvatarBase avatar)
        {
            var viewPos = Vector3.zero;
            var animator = avatar.Animator;
            var objTrans = avatar.Animator.transform;

            // leftEyeとRightEyeの位置からx, yを計算する
            var leftEyeTrans = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var rightEyeTrans = animator.GetBoneTransform(HumanBodyBones.RightEye);
            viewPos = (leftEyeTrans.position + rightEyeTrans.position) / 2f;

            var renderer = avatar.FaceMesh;
            var mesh = renderer.sharedMesh;
            var vertices = mesh.vertices;
            var local2WorldMat = renderer.transform.localToWorldMatrix;

            // 左目メッシュの頂点のうち, 左目ボーンから一番離れた頂点までの距離を計算する
            // 右目も同様に計算し, その平均距離+eyeBoneの平均z位置をzとする
            var leftMaxDistance = CalcDistanceFromMaxFarVertexTo(leftEyeTrans, renderer);
            var rightMaxDistance = CalcDistanceFromMaxFarVertexTo(rightEyeTrans, renderer);
            viewPos.z += (leftMaxDistance + rightMaxDistance) / 2f;

            var leftEyeBoneIndex = Array.IndexOf(renderer.bones, leftEyeTrans);
            var boneWeights = renderer.sharedMesh.boneWeights
                                .Where(x => x.boneIndex0 == leftEyeBoneIndex ||
                                            x.boneIndex1 == leftEyeBoneIndex ||
                                            x.boneIndex2 == leftEyeBoneIndex ||
                                            x.boneIndex3 == leftEyeBoneIndex)
                                .ToArray();

            // ローカル座標に変換
            viewPos = objTrans.worldToLocalMatrix.MultiplyPoint3x4(viewPos);

            return viewPos;
        }

        private static float CalcDistanceFromMaxFarVertexTo(Transform targetBone, SkinnedMeshRenderer renderer)
        {
            var targetBoneIndex = Array.IndexOf(renderer.bones, targetBone);
            var meshVertexIndices = renderer.sharedMesh.boneWeights
                                .Select((x, index) => new { index = index, value = x })
                                .Where(x => (x.value.boneIndex0 == targetBoneIndex && x.value.weight0 > 0f) ||
                                            (x.value.boneIndex1 == targetBoneIndex && x.value.weight1 > 0f) ||
                                            (x.value.boneIndex2 == targetBoneIndex && x.value.weight2 > 0f) ||
                                            (x.value.boneIndex3 == targetBoneIndex && x.value.weight3 > 0f))
                                .Select(x => x.index)
                                .ToArray();
            var maxDistance = 0f;
            var vertices = renderer.sharedMesh.vertices;
            var local2WorldMatrix = renderer.transform.localToWorldMatrix;
            foreach (var index in meshVertexIndices)
                if (maxDistance < Vector3.Distance(local2WorldMatrix.MultiplyPoint3x4(vertices[index]), targetBone.position))
                    maxDistance = Vector3.Distance(local2WorldMatrix.MultiplyPoint3x4(vertices[index]), targetBone.position);

            return maxDistance;
        }

        private static bool IsFaceMesh(Mesh mesh)
        {
            if (mesh == null) return false;

            var blendShapeNames = Enumerable.Range(0, mesh.blendShapeCount)
                                    .Select(index => mesh.GetBlendShapeName(index));
            foreach (var blendShapeName in blendShapeNames)
            {
                foreach (var prefix in lipSyncBlendShapeNamePrefixPatterns)
                {
                    if (blendShapeName.StartsWith(prefix, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
