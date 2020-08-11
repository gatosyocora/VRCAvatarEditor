using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class FaceEmotionGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar editAvatar;
        private VRCAvatarEditor.Avatar originalAvatar;
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

        public void Initialize(VRCAvatarEditor.Avatar editAvatar, VRCAvatarEditor.Avatar originalAvatar, string saveFolderPath, EditorWindow window, AnimationsGUI animationsGUI)
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
                using (new EditorGUI.DisabledScope(editAvatar.descriptor == null))
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

                if (editAvatar.skinnedMeshList != null)
                {
                    BlendShapeListGUI();
                }

                animName = EditorGUILayout.TextField(LocalizeText.instance.langPair.animClipFileNameLabel, animName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.animClipSaveFolderLabel, originalAvatar.animSavedFolderPath);

                    if (GUILayout.Button(LocalizeText.instance.langPair.selectFolder, GUILayout.Width(100)))
                    {
                        originalAvatar.animSavedFolderPath = EditorUtility.OpenFolderPanel(LocalizeText.instance.langPair.selectFolderDialogMessageText, originalAvatar.animSavedFolderPath, string.Empty);
                        originalAvatar.animSavedFolderPath = $"{FileUtil.GetProjectRelativePath(originalAvatar.animSavedFolderPath)}{Path.DirectorySeparatorChar}";
                        if (originalAvatar.animSavedFolderPath == $"{Path.DirectorySeparatorChar}") originalAvatar.animSavedFolderPath = $"Assets{Path.DirectorySeparatorChar}";
                        parentWindow.animationsGUI.UpdateSaveFolderPath(originalAvatar.animSavedFolderPath);
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
                        var animController = originalAvatar.standingAnimController;

                        var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(animName, originalAvatar.animSavedFolderPath, ref editAvatar, ref blendshapeExclusions, editAvatar.descriptor.gameObject);
                        if (selectedHandAnim != HandPose.HandPoseType.NoSelection)
                        {
                            HandPose.AddHandPoseAnimationKeysFromOriginClip(createdAnimClip, handPoseAnim);
                            animController[AnimationsGUI.HANDANIMS[(int)selectedHandAnim - 1]] = createdAnimClip;
                            EditorUtility.SetDirty(animController);

                            FaceEmotion.ResetToDefaultFaceEmotion(ref editAvatar);
                        }

                        originalAvatar.standingAnimController = animController;
                        editAvatar.standingAnimController = animController;

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
                foreach (var skinnedMesh in editAvatar.skinnedMeshList)
                {
                    skinnedMesh.isOpenBlendShapes = EditorGUILayout.Foldout(skinnedMesh.isOpenBlendShapes, skinnedMesh.obj.name);
                    if (skinnedMesh.isOpenBlendShapes)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            using (new GUILayout.HorizontalScope())
                            {
                                using (var check = new EditorGUI.ChangeCheckScope())
                                {
                                    skinnedMesh.isContainsAll = EditorGUILayout.ToggleLeft(string.Empty, skinnedMesh.isContainsAll, GUILayout.Width(45));
                                    if (check.changed)
                                    {
                                        FaceEmotion.SetContainsAll(skinnedMesh.isContainsAll, ref skinnedMesh.blendshapes);
                                    }
                                }
                                EditorGUILayout.LabelField(LocalizeText.instance.langPair.toggleAllLabel, GUILayout.Height(20));
                            }

                            foreach (var blendshape in skinnedMesh.blendshapes)
                            {
                                if (!blendshape.isExclusion)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        blendshape.isContains = EditorGUILayout.ToggleLeft(string.Empty, blendshape.isContains, GUILayout.Width(45));

                                        EditorGUILayout.SelectableLabel(blendshape.name, GUILayout.Height(20));
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            var value = skinnedMesh.renderer.GetBlendShapeWeight(blendshape.id);
                                            value = EditorGUILayout.Slider(value, 0, 100);
                                            if (check.changed)
                                                skinnedMesh.renderer.SetBlendShapeWeight(blendshape.id, value);
                                        }

                                        if (GUILayout.Button(LocalizeText.instance.langPair.minButtonText, GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMinValue(ref skinnedMesh.renderer, blendshape.id);
                                        }
                                        if (GUILayout.Button(LocalizeText.instance.langPair.maxButtonText, GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMaxValue(ref skinnedMesh.renderer, blendshape.id);
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
                var animController = originalAvatar.standingAnimController;
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
