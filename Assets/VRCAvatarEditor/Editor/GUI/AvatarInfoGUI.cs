using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK2
using VRCSDK2;
using VRCAvatar = VRCAvatarEditor.Avatars2.VRCAvatar2;
#elif VRC_SDK_VRCSDK3
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;
#endif
using LipSyncStyle = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle;
using AnimationSet = VRC.SDKBase.VRC_AvatarDescriptor.AnimationSet;
using Viseme = VRC.SDKBase.VRC_AvatarDescriptor.Viseme;
using VRCAvatarEditor.Utilities;

namespace VRCAvatarEditor
{
    public class AvatarInfoGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatar originalAvatar;
        private VRCAvatar editAvatar;

        private AvatarMonitorGUI monitorGUI;

        private bool isOpeningLipSync = false;
        private Vector2 lipSyncScrollPos = Vector2.zero;

        private Texture2D showIconTexture;
        private Texture2D hideIconTexture;

        public void Initialize(VRCAvatar originalAvatar, VRCAvatar editAvatar, AvatarMonitorGUI monitorGUI)
        {
            this.originalAvatar = originalAvatar;
            this.editAvatar = editAvatar;
            this.monitorGUI = monitorGUI;

            showIconTexture = Resources.Load<Texture2D>("Icon/ShowIcon");
            hideIconTexture = Resources.Load<Texture2D>("Icon/HideIcon");
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.avatarInfoTitle, EditorStyles.boldLabel);

            if (originalAvatar != null && originalAvatar.Descriptor != null)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    // 性別
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        originalAvatar.Sex = (AnimationSet)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.genderLabel, originalAvatar.Sex);

                        if (check.changed)
                        {
                            originalAvatar.Descriptor.Animations = originalAvatar.Sex;
                            EditorUtility.SetDirty(originalAvatar.Descriptor);
                        }
                    }

                    // アップロード状態
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.uploadStatusLabel,
                        (string.IsNullOrEmpty(originalAvatar.AvatarId)) ?
                            LocalizeText.instance.langPair.newAvatarText :
                            LocalizeText.instance.langPair.uploadedAvatarText);

                    // AnimatorOverrideController
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
#if VRC_SDK_VRCSDK2
                        originalAvatar.StandingAnimController = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.customStandingAnimsLabel,
                            originalAvatar.StandingAnimController);

                        originalAvatar.SittingAnimController = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.customSittingAnimsLabel,
                            originalAvatar.SittingAnimController);
#elif VRC_SDK_VRCSDK3
                        originalAvatar.GestureController = GatoGUILayout.ObjectField(
                            "Gesture Layer",
                            originalAvatar.GestureController);

                        originalAvatar.FxController = GatoGUILayout.ObjectField(
                            "FX Layer",
                            originalAvatar.FxController);
#endif

                        if (check.changed)
                        {
#if VRC_SDK_VRCSDK2
                            originalAvatar.Descriptor.CustomStandingAnims = originalAvatar.StandingAnimController;
                            originalAvatar.Descriptor.CustomSittingAnims = originalAvatar.SittingAnimController;
#elif VRC_SDK_VRCSDK3
                            originalAvatar.Descriptor.baseAnimationLayers[2].animatorController = originalAvatar.GestureController;
                            originalAvatar.Descriptor.baseAnimationLayers[4].animatorController = originalAvatar.FxController;
#endif
                            EditorUtility.SetDirty(originalAvatar.Descriptor);

                            originalAvatar.SetAnimSavedFolderPath();
                        }
                    }

                    // ポリゴン数
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.triangleCountLabel, originalAvatar.TriangleCount + "(" + (originalAvatar.TriangleCount + originalAvatar.TriangleCountInactive) + ")");

                    // 身長
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.heightLabel, $"{originalAvatar.Height:F2} m");

                    // View Position
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(LocalizeText.instance.langPair.viewPositionLabel, GUILayout.Width(145f));
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            originalAvatar.EyePos = EditorGUILayout.Vector3Field(string.Empty, originalAvatar.EyePos);

                            if (check.changed)
                            {
                                originalAvatar.Descriptor.ViewPosition = originalAvatar.EyePos;
                                editAvatar.Descriptor.ViewPosition = originalAvatar.EyePos;
                                editAvatar.EyePos = originalAvatar.EyePos;
                                EditorUtility.SetDirty(originalAvatar.Descriptor);

                                monitorGUI.showEyePosition = true;
                            }
                        }

                        monitorGUI.showEyePosition = GatoGUILayout.ToggleImage(
                                                        monitorGUI.showEyePosition,
                                                        showIconTexture,
                                                        hideIconTexture);

                        GatoGUILayout.Button(
                            "Auto Detect",
                            () => 
                            {
                                var eyePos = VRCAvatarMeshUtility.CalcAvatarViewPosition(originalAvatar);
                                originalAvatar.Descriptor.ViewPosition = eyePos;
                                editAvatar.Descriptor.ViewPosition = eyePos;
                                originalAvatar.EyePos = eyePos;
                                editAvatar.EyePos = eyePos;
                                EditorUtility.SetDirty(originalAvatar.Descriptor);

                                monitorGUI.showEyePosition = true;
                                monitorGUI.MoveAvatarCam(false, true);
                            },
                            originalAvatar.FaceMesh != null,
                            GUILayout.MaxWidth(100));
                    }
                    GatoGUILayout.ErrorBox(
                        "ViewPositionを自動設定するためにはFaceMeshを設定する必要があります",
                        originalAvatar.FaceMesh != null,
                        MessageType.Warning);

                    // faceMesh
                    using (new EditorGUILayout.HorizontalScope())
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        originalAvatar.FaceMesh = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.faceMeshLabel,
                            originalAvatar.FaceMesh);

                        GatoGUILayout.Button(
                            "Auto Detect",
                            () => { originalAvatar.FaceMesh = VRCAvatarMeshUtility.GetFaceMeshRenderer(originalAvatar); },
                            true,
                            GUILayout.MaxWidth(100));

                        if (check.changed)
                        {
                            originalAvatar.Descriptor.VisemeSkinnedMesh = originalAvatar.FaceMesh;
                            EditorUtility.SetDirty(originalAvatar.Descriptor);
                        }
                    }

                    EditorGUILayout.Space();

                    // リップシンク
                    using (new EditorGUILayout.HorizontalScope())
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        originalAvatar.LipSyncStyle = (LipSyncStyle)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.lipSyncTypeLabel, originalAvatar.LipSyncStyle);

                        using (new EditorGUI.DisabledGroupScope(originalAvatar.LipSyncStyle == LipSyncStyle.VisemeBlendShape && originalAvatar.FaceMesh != null))
                        {
                            GatoGUILayout.Button(
                                LocalizeText.instance.langPair.lipSyncBlendShapesAutoDetectButtonText,
                                () =>
                                {
                                    originalAvatar.SetLipSyncToViseme();
                                    EditorUtility.SetDirty(originalAvatar.Descriptor);
                                });
                        }

                        if (check.changed) originalAvatar.Descriptor.lipSync = originalAvatar.LipSyncStyle;
                    }
                    if (originalAvatar.LipSyncStyle == LipSyncStyle.VisemeBlendShape)
                    {
                        if (originalAvatar.FaceMesh != null)
                        {
                            isOpeningLipSync = EditorGUILayout.Foldout(isOpeningLipSync, LocalizeText.instance.langPair.lipSyncBlendShapesLabel);
                            if (isOpeningLipSync)
                            {
                                using (new EditorGUI.IndentLevelScope())
                                using (var scrollView = new EditorGUILayout.ScrollViewScope(lipSyncScrollPos))
                                {
                                    lipSyncScrollPos = scrollView.scrollPosition;

                                    var mesh = originalAvatar.FaceMesh.sharedMesh;
                                    var blendShapeNames = Enumerable.Range(0, mesh.blendShapeCount)
                                                            .Select(index => mesh.GetBlendShapeName(index))
                                                            .ToArray();
                                    for (int visemeIndex = 0; visemeIndex < VRCAvatarMeshUtility.LIPSYNC_SHYPEKEY_NUM; visemeIndex++)
                                    {
                                        var index = Array.IndexOf(blendShapeNames, originalAvatar.Descriptor.VisemeBlendShapes[visemeIndex]);
                                        var newIndex = EditorGUILayout.Popup("Viseme:" + Enum.GetName(typeof(Viseme), visemeIndex), index, blendShapeNames);
                                        if (index != newIndex)
                                        {
                                            originalAvatar.Descriptor.VisemeBlendShapes[visemeIndex] = blendShapeNames[newIndex];
                                        }
                                    }
                                }
                            }
                        }
                    }
                    GatoGUILayout.ErrorBox(
                        LocalizeText.instance.langPair.lipSyncWarningMessageText,
                        originalAvatar.LipSyncStyle == LipSyncStyle.VisemeBlendShape && originalAvatar.FaceMesh != null,
                        MessageType.Warning);
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

