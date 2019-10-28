using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCSDK2;
using UnityEditor;
using System;

namespace VRCAvatarEditor
{
    public class AvatarInfoGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar avatar;

        private bool isOpeningLipSync = false;
        private Vector2 lipSyncScrollPos = Vector2.zero;
        private const int LIPSYNC_SHYPEKEY_NUM = 15;

        public AvatarInfoGUI(ref VRCAvatarEditor.Avatar avatar)
        {
            this.avatar = avatar;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            if (avatar.descriptor != null)
            {
                // 性別
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.sex = (VRC_AvatarDescriptor.AnimationSet)EditorGUILayout.EnumPopup("Gender", avatar.sex);

                    if (check.changed) avatar.descriptor.Animations = avatar.sex;
                }

                // アップロード状態
                EditorGUILayout.LabelField("Status", (string.IsNullOrEmpty(avatar.avatarId)) ? "New Avatar" : "Uploaded Avatar");
                avatar.animator.runtimeAnimatorController = EditorGUILayout.ObjectField(
                    "Animator",
                    avatar.animator.runtimeAnimatorController,
                    typeof(AnimatorOverrideController),
                    true
                ) as RuntimeAnimatorController;

                // AnimatorOverrideController
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.standingAnimController = EditorGUILayout.ObjectField(
                        "Standing Animations",
                        avatar.standingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;
                    avatar.sittingAnimController = EditorGUILayout.ObjectField(
                        "Sitting Animations",
                        avatar.sittingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;

                    if (check.changed)
                    {
                        avatar.descriptor.CustomStandingAnims = avatar.standingAnimController;
                        avatar.descriptor.CustomSittingAnims = avatar.sittingAnimController;
                    }
                }

                EditorGUILayout.LabelField("Triangles", avatar.triangleCount + "(" + (avatar.triangleCount + avatar.triangleCountInactive) + ")");

                // リップシンク
                string lipSyncWarningMessage = "リップシンクが正しく設定されていない可能性があります";
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.lipSyncStyle = (VRC_AvatarDescriptor.LipSyncStyle)EditorGUILayout.EnumPopup("LipSync", avatar.lipSyncStyle);

                    if (check.changed) avatar.descriptor.lipSync = avatar.lipSyncStyle;
                }
                if (avatar.lipSyncStyle == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        avatar.faceMesh = EditorGUILayout.ObjectField(
                            "Face Mesh",
                            avatar.faceMesh,
                            typeof(SkinnedMeshRenderer),
                            true
                        ) as SkinnedMeshRenderer;

                        if (check.changed)
                            avatar.descriptor.VisemeSkinnedMesh = avatar.faceMesh;
                    }
                    if (avatar.faceMesh != null)
                    {
                        isOpeningLipSync = EditorGUILayout.Foldout(isOpeningLipSync, "ShapeKeys");
                        if (isOpeningLipSync)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            using (var scrollView = new EditorGUILayout.ScrollViewScope(lipSyncScrollPos))
                            {
                                lipSyncScrollPos = scrollView.scrollPosition;

                                for (int visemeIndex = 0; visemeIndex < LIPSYNC_SHYPEKEY_NUM; visemeIndex++)
                                {
                                    EditorGUILayout.LabelField("Viseme:" + Enum.GetName(typeof(VRC_AvatarDescriptor.Viseme), visemeIndex), avatar.descriptor.VisemeBlendShapes[visemeIndex]);
                                }
                            }
                        }
                    }
                }
                if (avatar.lipSyncStyle != VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape || avatar.faceMesh == null)
                {
                    EditorGUILayout.HelpBox(lipSyncWarningMessage, MessageType.Warning);
                    if (GUILayout.Button("シェイプキーによるリップシンクを自動設定する"))
                    {
                        avatar.SetLipSyncToViseme();
                    }
                }

                EditorGUILayout.Space();
            }
            return false;
        }

        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }
    }
}

