using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class AnimationsGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar editAvatar;
        private VRCAvatarEditor.Avatar originalAvatar;
        private VRCAvatarEditorGUI vrcAvatarEditorGUI;
        private FaceEmotionGUI faceEmotionGUI;

        public static readonly string[] HANDANIMS = { "FIST", "FINGERPOINT", "ROCKNROLL", "HANDOPEN", "THUMBSUP", "VICTORY", "HANDGUN" };
        public static readonly string[] EMOTEANIMS = { "EMOTE1", "EMOTE2", "EMOTE3", "EMOTE4", "EMOTE5", "EMOTE6", "EMOTE7", "EMOTE8" };

        private bool[] pathMissing = new bool[7];

        private GUIStyle normalStyle = new GUIStyle();
        private GUIStyle errorStyle = new GUIStyle();

        string titleText;
        AnimatorOverrideController controller;
        private bool showEmoteAnimations = false;

        private Tab _tab = Tab.Standing;

        private enum Tab
        {
            Standing,
            Sitting,
        }

        private string saveFolderPath;

        public void Initialize(ref VRCAvatarEditor.Avatar editAvatar,
                               VRCAvatarEditor.Avatar originalAvatar,
                               string saveFolderPath,
                               VRCAvatarEditorGUI vrcAvatarEditorGUI,
                               FaceEmotionGUI faceEmotionGUI)
        {
            this.editAvatar = editAvatar;
            this.originalAvatar = originalAvatar;
            this.vrcAvatarEditorGUI = vrcAvatarEditorGUI;
            this.faceEmotionGUI = faceEmotionGUI;
            UpdateSaveFolderPath(saveFolderPath);

            errorStyle.normal.textColor = Color.red;

            if (editAvatar != null && editAvatar.standingAnimController != null)
            {
                ValidateAnimatorOverrideController(editAvatar.animator, editAvatar.standingAnimController);
            }
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            // 設定済みアニメーション一覧
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, layoutOptions))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    // タブを描画する
                    _tab = (Tab)GUILayout.Toolbar((int)_tab, LocalizeText.instance.animationTabTexts, "LargeButton", GUI.ToolbarButtonSize.Fixed);
                    GUILayout.FlexibleSpace();
                }
                if (_tab == Tab.Standing)
                {
                    titleText = LocalizeText.instance.langPair.standingTabText;
                    if (originalAvatar != null)
                        controller = originalAvatar.standingAnimController;
                }
                else
                {
                    titleText = LocalizeText.instance.langPair.sittingTabText;
                    if (originalAvatar != null)
                        controller = originalAvatar.sittingAnimController;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(titleText, EditorStyles.boldLabel);

                    string btnText;
                    if (!showEmoteAnimations)
                    {
                        btnText = LocalizeText.instance.langPair.emoteButtonText;
                    }
                    else
                    {
                        btnText = LocalizeText.instance.langPair.faceAndHandButtonText;
                    }

                    if (GUILayout.Button(btnText))
                    {
                        showEmoteAnimations = !showEmoteAnimations;
                    }
                }

                EditorGUILayout.Space();

                if (controller != null)
                {
                    if (!showEmoteAnimations)
                    {
                        AnimationClip anim;
                        for (int i = 0; i < HANDANIMS.Length; i++)
                        {
                            var handPoseName = HANDANIMS[i];
                            if (handPoseName == controller[handPoseName].name)
                                anim = null;
                            else
                                anim = controller[handPoseName];

                            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(350)))
                            {
                                GUILayout.Label((i + 1) + ":" + handPoseName, (pathMissing[i]) ? errorStyle : normalStyle, GUILayout.Width(100));

                                controller[handPoseName] = EditorGUILayout.ObjectField(
                                    string.Empty,
                                    anim,
                                    typeof(AnimationClip),
                                    true,
                                    GUILayout.Width(200)
                                ) as AnimationClip;

                                using (new EditorGUI.DisabledGroupScope(anim == null))
                                {
                                    if (GUILayout.Button(LocalizeText.instance.langPair.edit, GUILayout.Width(50)))
                                    {
                                        if (vrcAvatarEditorGUI.currentTool != VRCAvatarEditorGUI.ToolFunc.FaceEmotion)
                                        {
                                            vrcAvatarEditorGUI.currentTool = VRCAvatarEditorGUI.ToolFunc.FaceEmotion;
                                            vrcAvatarEditorGUI.TabChanged();
                                        }
                                        FaceEmotion.ApplyAnimationProperties(controller[handPoseName], ref editAvatar);
                                        faceEmotionGUI.ChangeSaveAnimationState(controller[handPoseName].name,
                                            (HandPose.HandPoseType)Enum.ToObject(typeof(HandPose.HandPoseType), i + 1),
                                            controller[handPoseName]);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        AnimationClip anim;
                        foreach (var emoteAnim in EMOTEANIMS)
                        {
                            if (emoteAnim == controller[emoteAnim].name)
                                anim = null;
                            else
                                anim = controller[emoteAnim];

                            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(350)))
                            {
                                GUILayout.Label(emoteAnim, GUILayout.Width(90));

                                controller[emoteAnim] = EditorGUILayout.ObjectField(
                                    string.Empty,
                                    anim,
                                    typeof(AnimationClip),
                                    true,
                                    GUILayout.Width(250)
                                ) as AnimationClip;
                            }
                        }
                    }
                }
                else
                {
                    if (editAvatar.descriptor == null)
                    {
                        EditorGUILayout.HelpBox(LocalizeText.instance.langPair.noAvatarMessageText, MessageType.Warning);
                    }
                    else
                    {
                        string notSettingMessage, createMessage;
                        if (_tab == Tab.Standing)
                        {
                            notSettingMessage = LocalizeText.instance.langPair.noCustomStandingAnimsMessageText;
                            createMessage = LocalizeText.instance.langPair.createCustomStandingAnimsButtonText;
                        }
                        else
                        {
                            notSettingMessage = LocalizeText.instance.langPair.noCustomSittingAnimsMessageText;
                            createMessage = LocalizeText.instance.langPair.createCustomSittingAnimsButtonText;
                        }
                        EditorGUILayout.HelpBox(notSettingMessage, MessageType.Warning);

                        if (GUILayout.Button(createMessage))
                        {
                            var fileName = "CO_" + originalAvatar.animator.gameObject.name + ".overrideController";
                            saveFolderPath = "Assets/" + originalAvatar.animator.gameObject.name + "/";
                            var fullFolderPath = Path.GetFullPath(saveFolderPath);
                            if (!Directory.Exists(fullFolderPath))
                            {
                                Directory.CreateDirectory(fullFolderPath);
                                AssetDatabase.Refresh();
                            }
                            var createdCustomOverrideController = InstantiateVrcCustomOverideController(saveFolderPath + fileName);

                            if (_tab == Tab.Standing)
                            {
                                originalAvatar.descriptor.CustomStandingAnims = createdCustomOverrideController;
                                editAvatar.descriptor.CustomStandingAnims = createdCustomOverrideController;
                            }
                            else
                            {
                                originalAvatar.descriptor.CustomSittingAnims = createdCustomOverrideController;
                                editAvatar.descriptor.CustomSittingAnims = createdCustomOverrideController;
                            }

                            originalAvatar.LoadAvatarInfo();
                            editAvatar.LoadAvatarInfo();
                        }

                        if (_tab == Tab.Sitting)
                        {
                            using (new EditorGUI.DisabledGroupScope(editAvatar.standingAnimController == null))
                            {
                                if (GUILayout.Button(LocalizeText.instance.langPair.setToSameAsCustomStandingAnimsButtonText))
                                {
                                    var customStandingAnimsController = originalAvatar.descriptor.CustomStandingAnims;
                                    originalAvatar.descriptor.CustomSittingAnims = customStandingAnimsController;
                                    editAvatar.descriptor.CustomSittingAnims = customStandingAnimsController;
                                    originalAvatar.LoadAvatarInfo();
                                    editAvatar.LoadAvatarInfo();
                                }
                            }
                        }
                    }
                }

                if (pathMissing.Any(x => x))
                {
                    EditorGUILayout.HelpBox("Exist Missing Path in AnimationClip", MessageType.Warning);
                    if (GUILayout.Button("AutoFix"))
                    {
                        for (int i = 0; i < pathMissing.Length; i++)
                        {
                            if (!pathMissing[i]) continue;
                            pathMissing[i] = !GatoUtility.TryFixMissingPathInAnimationClip(
                                                editAvatar.animator, 
                                                editAvatar.standingAnimController[HANDANIMS[i]]);
                        }
                    }
                }
            }
            return false;
        }

        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }

        /// <summary>
        /// VRChat用のCustomOverrideControllerの複製を取得する
        /// </summary>
        /// <param name="animFolder"></param>
        /// <returns></returns>
        private AnimatorOverrideController InstantiateVrcCustomOverideController(string newFilePath)
        {
            string path = VRCAvatarEditorGUI.GetVRCSDKFilePath("CustomOverrideEmpty");

            newFilePath = AssetDatabase.GenerateUniqueAssetPath(newFilePath);
            AssetDatabase.CopyAsset(path, newFilePath);
            var overrideController = AssetDatabase.LoadAssetAtPath(newFilePath, typeof(AnimatorOverrideController)) as AnimatorOverrideController;

            return overrideController;
        }

        public void UpdateSaveFolderPath(string saveFolderPath)
        {
            this.saveFolderPath = saveFolderPath;
        }

        private void ValidateAnimatorOverrideController(Animator animator, AnimatorOverrideController controller)
        {
            for (int i = 0; i < HANDANIMS.Length; i++)
            {
                var clip = controller[HANDANIMS[i]];
                if (clip.name == HANDANIMS[i])
                {
                    pathMissing[i] = false;
                }
                else
                {
                    pathMissing[i] = !GatoUtility.ValidateMissingPathInAnimationClip(editAvatar.animator, clip);
                }
            }
        }
    }
}
