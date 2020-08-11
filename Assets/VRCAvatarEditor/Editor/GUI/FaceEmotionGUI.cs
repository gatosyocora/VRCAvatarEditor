using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Avatar = VRCAvatarEditor.VRCAvatar;

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
                using (new EditorGUI.DisabledScope(editAvatar.Descriptor == null))
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(LocalizeText.instance.langPair.loadAnimationButtonText))
                    {
                        FaceEmotion.LoadAnimationProperties(this, parentWindow);
                    }

                    if (GUILayout.Button(LocalizeText.instance.langPair.setToDefaultButtonText))
                    {
                        if (EditorUtility.DisplayDialog(
                                LocalizeText.instance.langPair.setToDefaultDialogTitleText,
                                LocalizeText.instance.langPair.setToDefaultDialogMessageText,
                                LocalizeText.instance.langPair.ok, LocalizeText.instance.langPair.cancel))
                        {
                            FaceEmotion.SetToDefaultFaceEmotion(ref editAvatar, originalAvatar);
                        }
                    }

                    if (GUILayout.Button(LocalizeText.instance.langPair.resetToDefaultButtonText))
                    {
                        FaceEmotion.ResetToDefaultFaceEmotion(ref editAvatar);
                        ChangeSaveAnimationState();
                    }
                }

                if (editAvatar.SkinnedMeshList != null)
                {
                    BlendShapeListGUI();
                }

                animName = EditorGUILayout.TextField(LocalizeText.instance.langPair.animClipFileNameLabel, animName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.animClipSaveFolderLabel, originalAvatar.AnimSavedFolderPath);

                    if (GUILayout.Button(LocalizeText.instance.langPair.selectFolder, GUILayout.Width(100)))
                    {
                        originalAvatar.AnimSavedFolderPath = EditorUtility.OpenFolderPanel(LocalizeText.instance.langPair.selectFolderDialogMessageText, originalAvatar.AnimSavedFolderPath, string.Empty);
                        originalAvatar.AnimSavedFolderPath = $"{FileUtil.GetProjectRelativePath(originalAvatar.AnimSavedFolderPath)}{Path.DirectorySeparatorChar}";
                        if (originalAvatar.AnimSavedFolderPath == $"{Path.DirectorySeparatorChar}") originalAvatar.AnimSavedFolderPath = $"Assets{Path.DirectorySeparatorChar}";
                        parentWindow.animationsGUI.UpdateSaveFolderPath(originalAvatar.AnimSavedFolderPath);
                    }

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
                    handPoseAnim = EditorGUILayout.ObjectField(LocalizeText.instance.langPair.handPoseAnimClipLabel, handPoseAnim, typeof(AnimationClip), true) as AnimationClip;
                }

                GUILayout.Space(20);

                using (new EditorGUI.DisabledGroupScope(
                            selectedHandAnim == HandPose.HandPoseType.NoSelection ||
                            handPoseAnim == null))
                {
                    if (GUILayout.Button(LocalizeText.instance.langPair.createAnimFileButtonText))
                    {
                        var animController = originalAvatar.StandingAnimController;

                        var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(animName, originalAvatar.AnimSavedFolderPath, ref editAvatar, ref blendshapeExclusions, editAvatar.Descriptor.gameObject);
                        if (selectedHandAnim != HandPose.HandPoseType.NoSelection)
                        {
                            HandPose.AddHandPoseAnimationKeysFromOriginClip(createdAnimClip, handPoseAnim);
                            animController[AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]] = createdAnimClip;
                            EditorUtility.SetDirty(animController);

                            FaceEmotion.ResetToDefaultFaceEmotion(ref editAvatar);
                        }

                        originalAvatar.StandingAnimController = animController;
                        editAvatar.StandingAnimController = animController;

                        animationsGUI.ResetPathMissing(AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]);
                    }
                }

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
                    if (GUILayout.Button(LocalizeText.instance.langPair.add))
                        blendshapeExclusions.Add(string.Empty);
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
            FaceEmotion.ResetToDefaultFaceEmotion(ref editAvatar);
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

                                        if (GUILayout.Button(LocalizeText.instance.langPair.minButtonText, GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMinValue(skinnedMesh.Renderer, blendshape.Id);
                                        }
                                        if (GUILayout.Button(LocalizeText.instance.langPair.maxButtonText, GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMaxValue(skinnedMesh.Renderer, blendshape.Id);
                                        }
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
            FaceEmotion.ApplyAnimationProperties(ScriptableSingleton<SendData>.instance.loadingProperties, ref editAvatar);
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
        }
    }
}
