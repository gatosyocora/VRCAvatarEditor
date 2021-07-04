﻿using System;
using System.Linq;
using UnityEngine;

namespace VRCAvatarEditor.Utilities
{
    public class VRCAvatarMeshUtility
    {
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

        private static bool IsFaceMesh(Mesh mesh)
        {
            if (mesh == null) return false;

            var faceMeshBlendShapeNamePrefix = "vrc.";

            var blendShapeNames = Enumerable.Range(0, mesh.blendShapeCount)
                                    .Select(index => mesh.GetBlendShapeName(index));
            foreach (var blendShapeName in blendShapeNames)
            {
                if (blendShapeName.StartsWith(faceMeshBlendShapeNamePrefix, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}