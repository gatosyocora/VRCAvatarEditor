using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCSDK2;
using System;
using VRC.Core;

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
        }
    }
}

