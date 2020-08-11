using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRCSDK2;

namespace VRCAvatarEditor
{
    public class VRCAvatar
    {
        public Animator Animator { get; set; }
        public VRC_AvatarDescriptor Descriptor { get; set; }
        public Vector3 EyePos { get; set; }
        public AnimatorOverrideController StandingAnimController { get; set; }
        public AnimatorOverrideController SittingAnimController { get; set; }
        public VRC_AvatarDescriptor.AnimationSet Sex { get; set; }
        public string AvatarId { get; set; }
        public int OverridesNum { get; set; }
        public SkinnedMeshRenderer FaceMesh { get; set; }
        public List<string> LipSyncShapeKeyNames { get; set; }
        public Material[] Materials { get; set; }
        public int TriangleCount { get; set; }
        public int TriangleCountInactive { get; set; }
        public VRC_AvatarDescriptor.LipSyncStyle LipSyncStyle { get; set; }
        public Enum FaceShapeKeyEnum { get; set; }
        public List<SkinnedMesh> SkinnedMeshList { get; set; }

        public List<SkinnedMeshRenderer> SkinnedMeshRendererList { get; set; }
        public List<MeshRenderer> MeshRendererList { get; set; }

        public string AnimSavedFolderPath { get; set; }

        public List<FaceEmotion.AnimParam> DefaultFaceEmotion { get; set; }

        public VRCAvatar()
        {
            Animator = null;
            Descriptor = null;
            EyePos = Vector3.zero;
            StandingAnimController = null;
            SittingAnimController = null;
            Sex = VRC_AvatarDescriptor.AnimationSet.None;
            AvatarId = string.Empty;
            OverridesNum = 0;
            FaceMesh = null;
            LipSyncShapeKeyNames = null;
            TriangleCount = 0;
            TriangleCountInactive = 0;
            LipSyncStyle = VRC_AvatarDescriptor.LipSyncStyle.Default;
            FaceShapeKeyEnum = null;
            SkinnedMeshList = null;
            AnimSavedFolderPath = $"Assets{Path.DirectorySeparatorChar}";
        }

        public VRCAvatar(VRC_AvatarDescriptor descriptor) : this()
        {
            LoadAvatarInfo(descriptor);
        }

        public void LoadAvatarInfo(VRC_AvatarDescriptor descriptor)
        {
            this.Descriptor = descriptor;
            LoadAvatarInfo();
        }

        /// <summary>
        /// アバターの情報を取得する
        /// </summary>
        public void LoadAvatarInfo()
        {
            if (Descriptor == null) return;

            var avatarObj = Descriptor.gameObject;

            Animator = avatarObj.GetComponent<Animator>();

            EyePos = Descriptor.ViewPosition;
            Sex = Descriptor.Animations;

            StandingAnimController = Descriptor.CustomStandingAnims;
            SittingAnimController = Descriptor.CustomSittingAnims;

            SetAnimSavedFolderPath();

            AvatarId = Descriptor.gameObject.GetComponent<PipelineManager>().blueprintId;

            FaceMesh = Descriptor.VisemeSkinnedMesh;

            if (FaceMesh != null && Descriptor.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
            {
                LipSyncShapeKeyNames = new List<string>();
                LipSyncShapeKeyNames.AddRange(Descriptor.VisemeBlendShapes);
            }

            Materials = GatoUtility.GetMaterials(avatarObj);

            int triangleCountInactive = 0;
            TriangleCount = GatoUtility.GetAllTrianglesCount(avatarObj, ref triangleCountInactive);
            this.TriangleCountInactive = triangleCountInactive;

            LipSyncStyle = Descriptor.lipSync;

            SkinnedMeshList = FaceEmotion.GetSkinnedMeshListOfBlendShape(avatarObj, FaceMesh.gameObject);

            SkinnedMeshRendererList = GatoUtility.GetSkinnedMeshList(avatarObj);
            MeshRendererList = GatoUtility.GetMeshList(avatarObj);

            DefaultFaceEmotion = FaceEmotion.GetAvatarFaceParamaters(SkinnedMeshList);
        }

        /// <summary>
        /// Avatarにシェイプキー基準のLipSyncの設定をおこなう
        /// </summary>
        public void SetLipSyncToViseme()
        {
            if (Descriptor == null) return;

            LipSyncStyle = VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
            Descriptor.lipSync = VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;

            if (FaceMesh == null)
            {
                var rootObj = Animator.gameObject;
                FaceMesh = rootObj.GetComponentInChildren<SkinnedMeshRenderer>();
                Descriptor.VisemeSkinnedMesh = FaceMesh;
            }

            if (FaceMesh == null) return;
            var mesh = FaceMesh.sharedMesh;

            var visemeBlendShapeNames = Enum.GetNames(typeof(VRC_AvatarDescriptor.Viseme));

            for (int visemeIndex = 0; visemeIndex < visemeBlendShapeNames.Length; visemeIndex++)
            {
                // VRC用アバターとしてよくあるシェイプキーの名前を元に自動設定
                var visemeShapeKeyName = "vrc.v_" + visemeBlendShapeNames[visemeIndex];
                if (mesh.GetBlendShapeIndex(visemeShapeKeyName) != -1)
                {
                    Descriptor.VisemeBlendShapes[visemeIndex] = visemeShapeKeyName;
                    continue;
                }

                visemeShapeKeyName = "VRC.v_" + visemeBlendShapeNames[visemeIndex];
                if (mesh.GetBlendShapeIndex(visemeShapeKeyName) != -1)
                {
                    Descriptor.VisemeBlendShapes[visemeIndex] = visemeShapeKeyName;
                }
            }
        }

        public void SetAnimSavedFolderPath()
        {
            if (StandingAnimController != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(StandingAnimController);
                AnimSavedFolderPath = $"{Path.GetDirectoryName(assetPath)}{Path.DirectorySeparatorChar}";
            }
        }
    }
}

