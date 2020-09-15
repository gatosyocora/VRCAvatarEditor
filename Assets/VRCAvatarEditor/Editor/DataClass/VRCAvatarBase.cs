using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using LipSyncStyle = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle;
using AnimationSet = VRC.SDKBase.VRC_AvatarDescriptor.AnimationSet;
using System.Linq;

namespace VRCAvatarEditor.Base
{
    public abstract class VRCAvatarBase : IVRCAvatarBase
    {
        public List<FaceEmotion.AnimParam> DefaultFaceEmotion { get; set; }
        public Animator Animator { get; set; }
        public Vector3 EyePos { get; set; }
        public string AvatarId { get; set; }
        public int OverridesNum { get; set; }
        public SkinnedMeshRenderer FaceMesh { get; set; }
        public List<string> LipSyncShapeKeyNames { get; set; }
        public Material[] Materials { get; set; }
        public int TriangleCount { get; set; }
        public int TriangleCountInactive { get; set; }
        public LipSyncStyle LipSyncStyle { get; set; }
        public AnimationSet Sex { get; set; }
        public Enum FaceShapeKeyEnum { get; set; }
        public List<SkinnedMesh> SkinnedMeshList { get; set; }
        public List<MeshRenderer> MeshRendererList { get; set; }
        public string AnimSavedFolderPath { get; set; }

        public VRCAvatarBase()
        {
            Animator = null;
            EyePos = Vector3.zero;
            AvatarId = string.Empty;
            OverridesNum = 0;
            FaceMesh = null;
            LipSyncShapeKeyNames = null;
            TriangleCount = 0;
            TriangleCountInactive = 0;
            FaceShapeKeyEnum = null;
            // TODO: CustomStandingAnimsが未設定の3Dモデルではぬるぽが出るのでインスタンス化して一時対処
            SkinnedMeshList = new List<SkinnedMesh>();
            AnimSavedFolderPath = $"Assets{Path.DirectorySeparatorChar}";
            Sex = AnimationSet.None;
            LipSyncStyle = LipSyncStyle.Default;
        }

        /// <summary>
        /// アバターの情報を取得する
        /// </summary>
        public void LoadAvatarInfo(GameObject avatarObj)
        {
            if (avatarObj == null) return;

            Animator = avatarObj.GetComponent<Animator>();
            AvatarId = avatarObj.GetComponent<PipelineManager>()?.blueprintId ?? string.Empty;
            Materials = GatoUtility.GetMaterials(avatarObj);

            int triangleCountInactive = 0;
            TriangleCount = GatoUtility.GetAllTrianglesCount(avatarObj, ref triangleCountInactive);
            TriangleCountInactive = triangleCountInactive;

            GameObject faceMeshObj = null;
            if (FaceMesh != null)
            {
                faceMeshObj = FaceMesh.gameObject;
                DefaultFaceEmotion = FaceEmotion.GetAvatarFaceParamaters(SkinnedMeshList);
            }

            SkinnedMeshList = GetSkinnedMeshList(avatarObj, faceMeshObj);
            MeshRendererList = GatoUtility.GetMeshList(avatarObj);
        }

        /// <summary>
        /// Avatarにシェイプキー基準のLipSyncの設定をおこなう
        /// </summary>
        public abstract void SetLipSyncToViseme();

        /// <summary>
        /// 与えられたアセットと同じ場所をアセットの保存先として設定する
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        public string GetAnimSavedFolderPath(UnityEngine.Object asset)
        {
            if (asset == null) return $"Assets{Path.DirectorySeparatorChar}";

            var assetPath = AssetDatabase.GetAssetPath(asset);
            return $"{Path.GetDirectoryName(assetPath)}{Path.DirectorySeparatorChar}";
        }

        /// <summary>
        /// 指定オブジェクト以下のSkinnedMeshRendererのリストを取得する
        /// </summary>
        /// <param name="parentObj">親オブジェクト</param>
        /// <returns>SkinnedMeshRendererのリスト</returns>
        public static List<SkinnedMesh> GetSkinnedMeshList(GameObject parentObj, GameObject faceMeshObj) =>
            parentObj.GetComponentsInChildren<SkinnedMeshRenderer>()
                .Where(s => s != null && s.sharedMesh != null)
                .Select(s => new SkinnedMesh(s, faceMeshObj))
                .ToList();
    }
}

