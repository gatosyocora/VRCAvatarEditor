using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCSDK2;
using System;
using VRC.Core;
using UnityEditor;
using System.IO;

namespace VRCAvatarEditor
{
    public class Avatar
    {
        public Animator animator { get; set; }
        public VRC_AvatarDescriptor descriptor { get; set; }
        public Vector3 eyePos { get; set; }
        public AnimatorOverrideController standingAnimController { get; set; }
        public AnimatorOverrideController sittingAnimController { get; set; }
        public VRC_AvatarDescriptor.AnimationSet sex { get; set; }
        public string avatarId { get; set; }
        public int overridesNum { get; set; }
        public SkinnedMeshRenderer faceMesh { get; set; }
        public List<string> lipSyncShapeKeyNames;
        public List<Material> materials { get; set; }
        public int triangleCount { get; set; }
        public int triangleCountInactive { get; set; }
        public VRC_AvatarDescriptor.LipSyncStyle lipSyncStyle { get; set; }
        public Enum faceShapeKeyEnum { get; set; }
        public List<SkinnedMesh> skinnedMeshList { get; set; }

        public List<SkinnedMeshRenderer> skinnedMeshRendererList { get; set; }
        public List<MeshRenderer> meshRendererList { get; set; }

        public string animSavedFolderPath { get; set; }

        public List<FaceEmotion.AnimParam> defaultFaceEmotion { get; set; }

        public Avatar()
        {
            animator = null;
            descriptor = null;
            eyePos = Vector3.zero;
            standingAnimController = null;
            sittingAnimController = null;
            sex = VRC_AvatarDescriptor.AnimationSet.None;
            avatarId = string.Empty;
            overridesNum = 0;
            faceMesh = null;
            lipSyncShapeKeyNames = null;
            triangleCount = 0;
            triangleCountInactive = 0;
            lipSyncStyle = VRC_AvatarDescriptor.LipSyncStyle.Default;
            faceShapeKeyEnum = null;
            skinnedMeshList = null;
            animSavedFolderPath = "Assets/";
        }

        /// <summary>
        /// アバターの情報を取得する
        /// </summary>
        public void LoadAvatarInfo()
        {
            if (descriptor == null) return;

            var avatarObj = descriptor.gameObject;

            animator = avatarObj.GetComponent<Animator>();

            eyePos = descriptor.ViewPosition;
            sex = descriptor.Animations;

            standingAnimController = descriptor.CustomStandingAnims;
            sittingAnimController = descriptor.CustomSittingAnims;

            SetAnimSavedFolderPath();

            avatarId = descriptor.gameObject.GetComponent<PipelineManager>().blueprintId;

            faceMesh = descriptor.VisemeSkinnedMesh;

            if (faceMesh != null && descriptor.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
            {
                lipSyncShapeKeyNames = new List<string>();
                lipSyncShapeKeyNames.AddRange(descriptor.VisemeBlendShapes);
            }

            materials = GatoUtility.GetMaterials(avatarObj);

            int triangleCountInactive = 0;
            triangleCount = GatoUtility.GetAllTrianglesCount(avatarObj, ref triangleCountInactive);
            this.triangleCountInactive = triangleCountInactive;

            lipSyncStyle = descriptor.lipSync;

            skinnedMeshList = FaceEmotion.GetSkinnedMeshListOfBlendShape(avatarObj);
            
            skinnedMeshRendererList = GatoUtility.GetSkinnedMeshList(avatarObj);
            meshRendererList = GatoUtility.GetMeshList(avatarObj);

            defaultFaceEmotion = FaceEmotion.GetAvatarFaceParamaters(skinnedMeshList);
        }

        /// <summary>
        /// Avatarにシェイプキー基準のLipSyncの設定をおこなう
        /// </summary>
        public void SetLipSyncToViseme()
        {
            if (descriptor == null) return;

            lipSyncStyle = VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
            descriptor.lipSync = VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;

            if (faceMesh == null)
            {
                var rootObj = animator.gameObject;
                faceMesh = rootObj.GetComponentInChildren<SkinnedMeshRenderer>();
                descriptor.VisemeSkinnedMesh = faceMesh;
            }

            if (faceMesh == null) return;
            var mesh = faceMesh.sharedMesh;

            var visemeBlendShapeNames = Enum.GetNames(typeof(VRC_AvatarDescriptor.Viseme));

            for (int visemeIndex = 0; visemeIndex < visemeBlendShapeNames.Length; visemeIndex++)
            {
                // VRC用アバターとしてよくあるシェイプキーの名前を元に自動設定
                var visemeShapeKeyName = "vrc.v_" + visemeBlendShapeNames[visemeIndex];
                if (mesh.GetBlendShapeIndex(visemeShapeKeyName) != -1)
                {
                    descriptor.VisemeBlendShapes[visemeIndex] = visemeShapeKeyName;
                    continue;
                }

                visemeShapeKeyName = "VRC.v_" + visemeBlendShapeNames[visemeIndex];
                if (mesh.GetBlendShapeIndex(visemeShapeKeyName) != -1)
                {
                    descriptor.VisemeBlendShapes[visemeIndex] = visemeShapeKeyName;
                }
            }
        }

        public void SetAnimSavedFolderPath()
        {
            if (standingAnimController != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(standingAnimController);
                animSavedFolderPath = Path.GetDirectoryName(assetPath) + "/";
            }
        }
    }
}

