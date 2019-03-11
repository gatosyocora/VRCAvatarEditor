//-----------------------------------------------------------------------
// <copyright file="AvatarShapeKeyAttacher.cs" company="Shiranui_Isuzu">
//     Copyright (c) Shiranui_Isuzu. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

#pragma warning disable CS0649  //~は割り当てられません、常に規定値~を使用します

using VRCSDK2;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ReSharper disable once CheckNamespace
namespace VRCSettingExtension.Editor
{
    [RequireComponent(typeof(VRC_AvatarDescriptor))]
    [CustomEditor(typeof(VRC_AvatarDescriptor))]
    public class AvatarShapeKeyAttacher : AvatarDescriptorEditor
    {
        private VRC_AvatarDescriptor avatarDescriptor;

        private SkinnedMeshRenderer selectedMesh;
        private List<string> blendShapeNames;

        /// <summary>
        /// InspectorのGUIを更新
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var button = GUILayout.Button("Auto Attach");

            if (button)
            {
                this.CreateLipSync();
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        private void CreateLipSync()
        {
            if (this.avatarDescriptor == null)
            {
                this.avatarDescriptor = (VRC_AvatarDescriptor)this.target;
            }

            this.avatarDescriptor.lipSync = (VRC_AvatarDescriptor.LipSyncStyle)EditorGUILayout.EnumPopup("Lip Sync", this.avatarDescriptor.lipSync);


            if (this.avatarDescriptor.lipSync != VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape) return;


            this.avatarDescriptor.VisemeSkinnedMesh = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Face Mesh", this.avatarDescriptor.VisemeSkinnedMesh, typeof(SkinnedMeshRenderer), true);
            if (this.avatarDescriptor.VisemeSkinnedMesh == null) return;

            this.DetermineBlendShapeNames();

            if (this.avatarDescriptor.VisemeBlendShapes == null || this.avatarDescriptor.VisemeBlendShapes.Length != (int)VRC_AvatarDescriptor.Viseme.Count)
            {
                this.avatarDescriptor.VisemeBlendShapes = new string[(int)VRC_AvatarDescriptor.Viseme.Count];
            }

            var shapeNames = this.blendShapeNames.Where(x => x.ToLower().Contains("vrc.v_")).ToList();

            var viseme = (VRC_AvatarDescriptor.Viseme[])Enum.GetValues(typeof(VRC_AvatarDescriptor.Viseme));
            var visemeList = viseme.ToList().Select(x => x.ToString().ToLower()).ToList();

            this.avatarDescriptor.VisemeBlendShapes = visemeList.Select(x => shapeNames.FirstOrDefault(y => y.Contains(x))).Where(x => x != null).ToArray();


        }

        /// <summary>
        /// 
        /// </summary>
        private void DetermineBlendShapeNames()
        {
            if (this.avatarDescriptor.VisemeSkinnedMesh == null ||
                this.avatarDescriptor.VisemeSkinnedMesh == this.selectedMesh) return;

            this.blendShapeNames = new List<string>
            {
                "-none-"
            };

            this.selectedMesh = this.avatarDescriptor.VisemeSkinnedMesh;

            for (var i = 0; i < this.selectedMesh.sharedMesh.blendShapeCount; ++i)
            {
                this.blendShapeNames.Add(this.selectedMesh.sharedMesh.GetBlendShapeName(i));
            }
        }
    }
}