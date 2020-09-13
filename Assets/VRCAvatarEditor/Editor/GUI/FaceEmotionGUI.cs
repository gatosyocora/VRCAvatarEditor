using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK2
using VRCAvatar = VRCAvatarEditor.Avatars2.VRCAvatar2;
using AnimationsGUI = VRCAvatarEditor.Avatars2.AnimationsGUI2;
#else
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;
using AnimationsGUI = VRCAvatarEditor.Avatars3.AnimationsGUI3;
#endif

namespace VRCAvatarEditor
{
    public class FaceEmotionGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatar editAvatar;
        private VRCAvatar originalAvatar;
        private VRCAvatarEditorGUI parentWindow;
        private AnimationsGUI animationsGUI;

        private static readonly string DEFAULT_ANIM_NAME = "faceAnim";
        private HandPose.HandPoseType selectedHandAnim = HandPose.HandPoseType.NoSelection;

        private Vector2 scrollPos = Vector2.zero;

        public enum SortType
        {
            UnSort,
            AToZ,
        }

        public SortType selectedSortType = SortType.UnSort;
        public List<string> blendshapeExclusions = new List<string> { "vrc.v_", "vrc.blink_", "vrc.lowerlid_", "vrc.owerlid_", "mmd" };

        private bool isOpeningBlendShapeExclusionList = false;

        private string animName;

        private AnimationClip handPoseAnim;

        private bool usePreviousAnimationOnHandAnimation;

        public void Initialize(VRCAvatar editAvatar, VRCAvatar originalAvatar, string saveFolderPath, EditorWindow window, AnimationsGUI animationsGUI)
        {
            this.editAvatar = editAvatar;
            this.originalAvatar = originalAvatar;
            animName = DEFAULT_ANIM_NAME;
            this.parentWindow = window as VRCAvatarEditorGUI;
            this.animationsGUI = animationsGUI;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.faceEmotionTitle, EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.loadAnimationButtonText,
                        () => {
                            FaceEmotion.LoadAnimationProperties(this, parentWindow);
                        },
                        editAvatar.Descriptor != null);

                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.setToDefaultButtonText,
                        () => {
                            if (EditorUtility.DisplayDialog(
                                    LocalizeText.instance.langPair.setToDefaultDialogTitleText,
                                    LocalizeText.instance.langPair.setToDefaultDialogMessageText,
                                    LocalizeText.instance.langPair.ok, LocalizeText.instance.langPair.cancel))
                            {
                                FaceEmotion.SetToDefaultFaceEmotion(editAvatar, originalAvatar);
                            }
                        },
                        editAvatar.Descriptor != null);

                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.resetToDefaultButtonText,
                        () => {
                            FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
                            ChangeSaveAnimationState();
                        },
                        editAvatar.Descriptor != null);
                }

                if (editAvatar.SkinnedMeshList != null)
                {
                    BlendShapeListGUI();
                }

                animName = EditorGUILayout.TextField(LocalizeText.instance.langPair.animClipFileNameLabel, animName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.animClipSaveFolderLabel, originalAvatar.AnimSavedFolderPath);

                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.selectFolder,
                        () => {
                            originalAvatar.AnimSavedFolderPath = EditorUtility.OpenFolderPanel(LocalizeText.instance.langPair.selectFolderDialogMessageText, originalAvatar.AnimSavedFolderPath, string.Empty);
                            originalAvatar.AnimSavedFolderPath = $"{FileUtil.GetProjectRelativePath(originalAvatar.AnimSavedFolderPath)}{Path.DirectorySeparatorChar}";
                            if (originalAvatar.AnimSavedFolderPath == $"{Path.DirectorySeparatorChar}") originalAvatar.AnimSavedFolderPath = $"Assets{Path.DirectorySeparatorChar}";
                            parentWindow.animationsGUI.UpdateSaveFolderPath(originalAvatar.AnimSavedFolderPath);
                        },
                        true,
                        GUILayout.Width(100));
                }

                EditorGUILayout.Space();

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    selectedHandAnim = (HandPose.HandPoseType)Enum.ToObject(typeof(HandPose.HandPoseType), EditorGUILayout.Popup(
                        LocalizeText.instance.langPair.animationOverrideLabel,
                        (int)selectedHandAnim,
                        Enum.GetNames(typeof(HandPose.HandPoseType)).Select((x, index) => index + ":" + x).ToArray()));

                    if (check.changed)
                    {
                        ChangeSelectionHandAnimation();
                    }
                }

                using (new EditorGUI.DisabledGroupScope(selectedHandAnim == HandPose.HandPoseType.NoSelection))
                {
                    handPoseAnim = GatoGUILayout.ObjectField(
                                        LocalizeText.instance.langPair.handPoseAnimClipLabel,
                                        handPoseAnim);
                }

                GUILayout.Space(20);

                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.createAnimFileButtonText,
                    () => {
#if VRC_SDK_VRCSDK2
                        var animController = originalAvatar.StandingAnimController;

                        var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(animName, originalAvatar.AnimSavedFolderPath, editAvatar, blendshapeExclusions, editAvatar.Descriptor.gameObject);
                        if (selectedHandAnim != HandPose.HandPoseType.NoSelection)
                        {
                            HandPose.AddHandPoseAnimationKeysFromOriginClip(createdAnimClip, handPoseAnim);
                            animController[AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]] = createdAnimClip;
                            EditorUtility.SetDirty(animController);

                            FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
                        }

                        originalAvatar.StandingAnimController = animController;
                        editAvatar.StandingAnimController = animController;

                        animationsGUI.ResetPathMissing(AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]);
#endif
                    },
                    selectedHandAnim != HandPose.HandPoseType.NoSelection &&
                    handPoseAnim != null);
            }

            return false;
        }

        public void DrawSettingsGUI()
        {
            EditorGUILayout.LabelField("FaceEmotion Creator", EditorStyles.boldLabel);

            selectedSortType = (SortType)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.sortTypeLabel, selectedSortType);

            isOpeningBlendShapeExclusionList = EditorGUILayout.Foldout(isOpeningBlendShapeExclusionList, LocalizeText.instance.langPair.blendShapeExclusionsLabel);
            if (isOpeningBlendShapeExclusionList)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    for (int i = 0; i < blendshapeExclusions.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            blendshapeExclusions[i] = EditorGUILayout.TextField(blendshapeExclusions[i]);
                            if (GUILayout.Button(LocalizeText.instance.langPair.remove))
                                blendshapeExclusions.RemoveAt(i);
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.add,
                        () => {
                            blendshapeExclusions.Add(string.Empty);
                        });
                }
            }

            usePreviousAnimationOnHandAnimation = EditorGUILayout.ToggleLeft(LocalizeText.instance.langPair.usePreviousAnimationOnHandAnimationLabel, usePreviousAnimationOnHandAnimation);
        }

        public void LoadSettingData(SettingData settingAsset)
        {
            selectedSortType = settingAsset.selectedSortType;
            blendshapeExclusions = new List<string>(settingAsset.blendshapeExclusions);
            usePreviousAnimationOnHandAnimation = settingAsset.usePreviousAnimationOnHandAnimation;
        }

        public void SaveSettingData(ref SettingData settingAsset)
        {
            settingAsset.selectedSortType = selectedSortType;
            settingAsset.blendshapeExclusions = new List<string>(blendshapeExclusions);
            settingAsset.usePreviousAnimationOnHandAnimation = usePreviousAnimationOnHandAnimation;
        }

        public void Dispose()
        {
            FaceEmotion.ResetToDefaultFaceEmotion(editAvatar);
        }

        private void BlendShapeListGUI()
        {
            // BlendShapeのリスト
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scrollView.scrollPosition;
                foreach (var skinnedMesh in editAvatar.SkinnedMeshList)
                {
                    skinnedMesh.IsOpenBlendShapes = EditorGUILayout.Foldout(skinnedMesh.IsOpenBlendShapes, skinnedMesh.Obj.name);
                    if (skinnedMesh.IsOpenBlendShapes)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    skinnedMesh.IsContainsAll = EditorGUILayout.ToggleLeft(string.Empty, skinnedMesh.IsContainsAll, GUILayout.Width(45));
                                    if (check.changed)
                                    {
                                        FaceEmotion.SetContainsAll(skinnedMesh.IsContainsAll, skinnedMesh.Blendshapes);
                                    }
                                }
                                EditorGUILayout.LabelField(LocalizeText.instance.langPair.toggleAllLabel, GUILayout.Height(20));
                            }

                            foreach (var blendshape in skinnedMesh.Blendshapes)
                            {
                                if (!blendshape.IsExclusion)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        blendshape.IsContains = EditorGUILayout.ToggleLeft(string.Empty, blendshape.IsContains, GUILayout.Width(45));

                                        EditorGUILayout.SelectableLabel(blendshape.Name, GUILayout.Height(20));
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            var value = skinnedMesh.Renderer.GetBlendShapeWeight(blendshape.Id);
                                            value = EditorGUILayout.Slider(value, 0, 100);
                                            if (check.changed)
                                                skinnedMesh.Renderer.SetBlendShapeWeight(blendshape.Id, value);
                                        }

                                        GatoGUILayout.Button(
                                            LocalizeText.instance.langPair.minButtonText,
                                            () => {
                                                FaceEmotion.SetBlendShapeMinValue(skinnedMesh.Renderer, blendshape.Id);
                                            },
                                            true,
                                            GUILayout.MaxWidth(50));

                                        GatoGUILayout.Button(
                                            LocalizeText.instance.langPair.maxButtonText,
                                            () => {
                                                FaceEmotion.SetBlendShapeMaxValue(skinnedMesh.Renderer, blendshape.Id);
                                            },
                                            true,
                                            GUILayout.MaxWidth(50));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnLoadedAnimationProperties()
        {
            FaceEmotion.ApplyAnimationProperties(ScriptableSingleton<SendData>.instance.loadingProperties, editAvatar);
            ChangeSaveAnimationState();
        }

        public void ChangeSaveAnimationState(
                string animName = "",
                HandPose.HandPoseType selectedHandAnim = HandPose.HandPoseType.NoSelection,
                AnimationClip handPoseAnim = null)
        {
            this.animName = animName;
            this.selectedHandAnim = selectedHandAnim;
            if (handPoseAnim is null)
                handPoseAnim = HandPose.GetHandAnimationClip(selectedHandAnim);
            this.handPoseAnim = handPoseAnim;
        }

        private void ChangeSelectionHandAnimation()
        {
#if VRC_SDK_VRCSDK2
            if (usePreviousAnimationOnHandAnimation)
            {
                var animController = originalAvatar.StandingAnimController;
                var previousAnimation = animController[AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]];

                // 未設定でなければ以前設定されていたものをHandPoseAnimationとして使う
                if (previousAnimation != null && previousAnimation.name != AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1])
                {
                    handPoseAnim = previousAnimation;
                }
                else
                {
                    handPoseAnim = HandPose.GetHandAnimationClip(selectedHandAnim);
                }
            }
            else
            {
                handPoseAnim = HandPose.GetHandAnimationClip(selectedHandAnim);
            }
#endif
        }
    }
}
