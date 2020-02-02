using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

namespace VRCAvatarEditor
{
    public class AnimationsGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar avatar;

        private GUILayoutOption[] layoutOptions;

        public static readonly string[] HANDANIMS = { "FIST", "FINGERPOINT", "ROCKNROLL", "HANDOPEN", "THUMBSUP", "VICTORY", "HANDGUN" };
        public static readonly string[] EMOTEANIMS = { "EMOTE1", "EMOTE2", "EMOTE3", "EMOTE4", "EMOTE5", "EMOTE6", "EMOTE7", "EMOTE8" };

        private string kind;
        string titleText;
        AnimatorOverrideController controller;
        private bool showEmoteAnimations = false;
        
        private Tab _tab = Tab.Standing;

        private enum Tab
        {
            Standing,
            Sitting,
        }

        private static class Styles
        {
            private static GUIContent[] _tabToggles = null;
            public static GUIContent[] TabToggles
            {
                get
                {
                    if (_tabToggles == null)
                    {
                        _tabToggles = System.Enum.GetNames(typeof(Tab)).Select(x => new GUIContent(x)).ToArray();
                    }
                    return _tabToggles;
                }
            }

            public static readonly GUIStyle TabButtonStyle = "LargeButton";

            public static readonly GUI.ToolbarButtonSize TabButtonSize = GUI.ToolbarButtonSize.Fixed;
        }

        private string saveFolderPath;

        public AnimationsGUI(ref VRCAvatarEditor.Avatar avatar, string saveFolderPath)
        {
            this.avatar = avatar;
            UpdateSaveFolderPath(saveFolderPath);
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
                    _tab = (Tab)GUILayout.Toolbar((int)_tab, Styles.TabToggles, Styles.TabButtonStyle, Styles.TabButtonSize);
                    GUILayout.FlexibleSpace();
                }
                if (_tab == Tab.Standing)
                {
                    kind = "Standing";
                    controller = avatar.standingAnimController;
                }
                else
                {
                    kind = "Sitting";
                    controller = avatar.sittingAnimController;
                }

                titleText = kind + " Animations";

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(titleText, EditorStyles.boldLabel);

                    string btnText;
                    if (!showEmoteAnimations)
                    {
                        btnText = "Emote";
                    }
                    else
                    {
                        btnText = "Face&Hand";
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
                        foreach (var handAnim in HANDANIMS)
                        {
                            if (handAnim == controller[handAnim].name)
                                anim = null;
                            else
                                anim = controller[handAnim];

                            using (new EditorGUILayout.HorizontalScope(GUILayout.Width(350)))
                            {
                                GUILayout.Label(handAnim, GUILayout.Width(90));

                                controller[handAnim] = EditorGUILayout.ObjectField(
                                    string.Empty,
                                    anim,
                                    typeof(AnimationClip),
                                    true,
                                    GUILayout.Width(200)
                                ) as AnimationClip;

                                using (new EditorGUI.DisabledGroupScope(anim == null))
                                {
                                    if (GUILayout.Button("Edit", GUILayout.Width(50)))
                                    {
                                        FaceEmotion.ApplyAnimationProperties(controller[handAnim], ref avatar);
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
                    if (avatar.descriptor == null)
                    {
                        EditorGUILayout.HelpBox("Not Setting Avatar", MessageType.Warning);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Not Setting Custom " + kind + " Anims", MessageType.Warning);

                        if (_tab == Tab.Standing)
                        {
                            if (GUILayout.Button("Auto Setting"))
                            {
                                var fileName = "CO_" + avatar.animator.gameObject.name + ".overrideController";
                                saveFolderPath = "Assets/" + avatar.animator.gameObject.name + "/";
                                var fullFolderPath = Path.GetFullPath(saveFolderPath);
                                if (!Directory.Exists(fullFolderPath)) 
                                {
                                    Directory.CreateDirectory(fullFolderPath);
                                    AssetDatabase.Refresh();
                                }
                                avatar.descriptor.CustomStandingAnims = InstantiateVrcCustomOverideController(saveFolderPath + fileName);
                                avatar.LoadAvatarInfo();
                            }
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

        public void SetAnimationsAreaLayout(GUILayoutOption[] layoutOptions)
        {
            this.layoutOptions = layoutOptions;
        }

        public void UpdateSaveFolderPath (string saveFolderPath)
        {
            this.saveFolderPath = saveFolderPath;
        }
    }
}

