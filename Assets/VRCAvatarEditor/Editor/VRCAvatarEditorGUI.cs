using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using VRCAvatarEditor.Utilitys;
#if VRC_SDK_VRCSDK2
using VRCSDK2;
using VRCAvatar = VRCAvatarEditor.Avatars2.VRCAvatar2;
#elif VRC_SDK_VRCSDK3
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;
using VRC.SDK3.Avatars.Components;
using VRC_AvatarDescriptor = VRC.SDK3.Avatars.Components.VRCAvatarDescriptor;
#endif

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{
    public class VRCAvatarEditorGUI : EditorWindow
    {
        private const string TOOL_VERSION = "v0.5.1";
        private const string TWITTER_ID = "gatosyocora";
        private const string DISCORD_ID = "gatosyocora#9575";
        private const string MANUAL_URL = "https://docs.google.com/document/d/1DU7mP5PTvERqHzZiiCBJ9ep5CilQ1iaXC_3IoiuPEgA/edit?usp=sharing";
        private const string BOOTH_URL = "gatosyocora.booth.pm";
        private const string BOOTH_ITEM_URL = "https://booth.pm/ja/items/1258744";
        private static readonly string GITHUB_LATEST_RELEASE_API_URL = "https://api.github.com/repos/gatosyocora/VRCAvatarEditor/releases/latest";

        private AvatarMonitorGUI avatarMonitorGUI;
        public AnimationsGUI animationsGUI;
        private AvatarInfoGUI avatarInfoGUI;
        private FaceEmotionGUI faceEmotionGUI;
        private ProbeAnchorGUI probeAnchorGUI;
        private MeshBoundsGUI meshBoundsGUI;
        private ShaderGUI shaderGUI;
        private IVRCAvatarEditorGUI selectedToolGUI;
        private Dictionary<ToolFunc, IVRCAvatarEditorGUI> toolGUIs = new Dictionary<ToolFunc, IVRCAvatarEditorGUI>();

        private bool needRepaint = false;

        private VRC_AvatarDescriptor targetAvatarDescriptor;

        private VRCAvatar edittingAvatar = null;
        private VRCAvatar originalAvatar = null;

        private string editorFolderPath;

        private string saveFolder;

        public enum ToolFunc
        {
            AvatarInfo,
            FaceEmotion,
            ProbeAnchor,
            Bounds,
            Shader,
        }

        public ToolFunc currentTool;

        private GUILayoutOption[][] layoutOptions
                        = new GUILayoutOption[][]
                            {
                                new GUILayoutOption[]{ GUILayout.MinWidth(350), GUILayout.MaxHeight(270) },
                                new GUILayoutOption[]{ GUILayout.Height(200)}
                            };

        #region Shader Variable
        #endregion

        #region ToolInfo Variable

        private bool isShowingToolInfo = false;
        private const string LICENSE_FILE_NAME = "LICENSE.txt";
        private const string README_FILE_NAME = "README.txt";
        private const string USING_SOFTWARE_FILE_NAME = "USING_SOFTWARE_LICENSES.txt";
        private readonly string[] TOOL_FUNCS = { "Avatar Monitor", "SunLight Rotator", "FaceEmotion Creator", "HandPose Adder", "ProbeAnchor Setter", "MeshBounds Setter", "Shader Checker", "HumanoidPose Resetter" };
        private string licenseText;
        private string readmeText;
        private string usingSoftwareLicenseText;
        private bool isShowingLicense = false;
        private bool isShowingReadme = true;
        private bool isShowingUsingSoftwareLicense = false;

        private Vector2 licenseScrollPos = Vector2.zero;
        private Vector2 readmeScrollPos = Vector2.zero;
        private Vector2 usingSoftwareLicenseScrollPos = Vector2.zero;

        #endregion

        #region Setting Variable

        private bool isShowingSetting = false;

        public enum LayoutType
        {
            Default,
            Half,
        }

        #endregion

        #region Changeable Parameters from Setting

        private LayoutType layoutType = LayoutType.Default;
        private string language = "EN";

        #endregion


        [MenuItem("VRCAvatarEditor/Editor")]
        public static void Create()
        {
            var window = GetWindow<VRCAvatarEditorGUI>("VRCAvatarEditor");
            window.minSize = new Vector2(700f, 500f);
        }

        private void OnEnable()
        {
            edittingAvatar = new VRCAvatar();

            editorFolderPath = Path.GetDirectoryName(
                                    AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this)));
            editorFolderPath = editorFolderPath.Substring(0, editorFolderPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);

            saveFolder = "Assets/";


            licenseText = FileUtility.GetFileTexts(editorFolderPath + LICENSE_FILE_NAME);
            readmeText = FileUtility.GetFileTexts(editorFolderPath + README_FILE_NAME);
            usingSoftwareLicenseText = FileUtility.GetFileTexts(editorFolderPath + USING_SOFTWARE_FILE_NAME);

            avatarMonitorGUI = ScriptableObject.CreateInstance<AvatarMonitorGUI>();
            animationsGUI = ScriptableObject.CreateInstance<AnimationsGUI>();
            avatarInfoGUI = ScriptableObject.CreateInstance<AvatarInfoGUI>();
            faceEmotionGUI = ScriptableObject.CreateInstance<FaceEmotionGUI>();
            probeAnchorGUI = ScriptableObject.CreateInstance<ProbeAnchorGUI>();
            meshBoundsGUI = ScriptableObject.CreateInstance<MeshBoundsGUI>();
            shaderGUI = ScriptableObject.CreateInstance<ShaderGUI>();

            toolGUIs.Add(ToolFunc.AvatarInfo, avatarInfoGUI);
            toolGUIs.Add(ToolFunc.FaceEmotion, faceEmotionGUI);
            toolGUIs.Add(ToolFunc.ProbeAnchor, probeAnchorGUI);
            toolGUIs.Add(ToolFunc.Bounds, meshBoundsGUI);
            toolGUIs.Add(ToolFunc.Shader, shaderGUI);

            avatarMonitorGUI.Initialize(currentTool);
            animationsGUI.Initialize(edittingAvatar, originalAvatar, saveFolder, this, faceEmotionGUI);
            avatarInfoGUI.Initialize(originalAvatar);
            probeAnchorGUI.Initialize(originalAvatar);

            selectedToolGUI = avatarInfoGUI;
            currentTool = ToolFunc.AvatarInfo;

            (layoutType, language) = EditorSetting.instance.LoadSettingDataFromScriptableObject(
                                            editorFolderPath, language,
                                            avatarMonitorGUI, faceEmotionGUI);

            // Windowを開いたときにオブジェクトが選択されていればそれをアバターとして設定する
            if (Selection.gameObjects.Length == 1)
            {
                var selectionTransform = Selection.gameObjects.Single().transform;
                while (selectionTransform != null)
                {
                    targetAvatarDescriptor = selectionTransform.GetComponent<VRC_AvatarDescriptor>();
                    if (targetAvatarDescriptor != null)
                    {
                        OnChangedAvatar();
                        break;
                    }
                    selectionTransform = selectionTransform.parent;
                }
            }

            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        private void OnDisable()
        {
            avatarMonitorGUI.Dispose();
            SceneView.onSceneGUIDelegate -= OnSceneGUI;
        }

        private void OnDestroy()
        {
            faceEmotionGUI.Dispose();
        }

        private void OnGUI()
        {
            if (LocalizeText.instance.langPair is null)
            {
                DrawNowLoading();
                return;
            }

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(EditorGUIUtility.singleLineHeight * 1.5f)))
            {

                GUILayout.FlexibleSpace();

                GatoGUILayout.Button(
                    LocalizeText.instance.langPair.reloadAvatarButtonText,
                    () => {
                        OnChangedAvatar();
                    },
                    originalAvatar != null);

                EditorGUILayout.Space();

                var toolInfoButtonText = (!isShowingToolInfo) ? LocalizeText.instance.langPair.toolInfoButtonText : LocalizeText.instance.langPair.close;
                var settingButtonText = (!isShowingSetting) ? LocalizeText.instance.langPair.settingButtonText : LocalizeText.instance.langPair.close;
                GatoGUILayout.Button(
                    toolInfoButtonText,
                    () => {
                        isShowingToolInfo = !isShowingToolInfo;
                        isShowingSetting = false;
                    },
                    true,
                    GUILayout.MinWidth(50));

                GatoGUILayout.Button(
                    settingButtonText,
                    () =>
                    {
                        isShowingSetting = !isShowingSetting;
                        isShowingToolInfo = false;

                        if (!isShowingSetting)
                        {
                            EditorSetting.instance.ApplySettingsToEditorGUI(
                                edittingAvatar,
                                faceEmotionGUI);
                        }
                    },
                    true,
                    GUILayout.MinWidth(50));
            }

            if (!isShowingToolInfo && !isShowingSetting)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    // アバター選択
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        targetAvatarDescriptor = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.avatarLabel,
                            targetAvatarDescriptor);

                        if (check.changed)
                        {
                            // アバター変更時の処理
                            if (targetAvatarDescriptor != null)
                            {
                                OnChangedAvatar();
                            }
                        }
                    }

                    using (new EditorGUI.DisabledGroupScope(edittingAvatar.Descriptor == null))
                    {
                        // LayoutType: Default
                        if (layoutType == LayoutType.Default)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                needRepaint = avatarMonitorGUI.DrawGUI(null);
                                if (needRepaint)
                                {
                                    Repaint();
                                }
                                else
                                {
                                    animationsGUI.DrawGUI(layoutOptions[0]);
                                }
                            }

                            DrawToolSwitchTab();

                            selectedToolGUI.DrawGUI(null);
                        }
                        // LayoutType: Half
                        else
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                needRepaint = avatarMonitorGUI.DrawGUI(null);
                                if (needRepaint) Repaint();

                                using (new EditorGUILayout.VerticalScope())
                                {
                                    DrawToolSwitchTab();

                                    if (currentTool == ToolFunc.AvatarInfo)
                                    {
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            if (!needRepaint)
                                                animationsGUI.DrawGUI(layoutOptions[1]);
                                        }

                                        // アバター情報
                                        avatarInfoGUI.DrawGUI(null);

                                    }
                                    else
                                    {
                                        selectedToolGUI.DrawGUI(null);
                                    }
                                }
                            }
                        }

                        EditorGUILayout.Space();

                        // ポーズ修正
                        GatoGUILayout.Button(
                            LocalizeText.instance.langPair.resetPoseButtonText,
                            () => {
                                HumanoidPose.ResetPose(edittingAvatar.Descriptor.gameObject);
                                HumanoidPose.ResetPose(originalAvatar.Descriptor.gameObject);
                            });

                        // アップロード
                        GatoGUILayout.Button(
                            LocalizeText.instance.langPair.uploadAvatarButtonText,
                            () => {
                                VRCSDKUtility.UploadAvatar(VRCSDKUtility.IsNewSDKUI());
                            });
                    }
                }
            }
            else if (isShowingToolInfo)
            {
                ToolInfoGUI();
            }
            else
            {
                SettingGUI();
            }

            EditorGUILayout.Space();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (currentTool == ToolFunc.Bounds)
            {
                if (meshBoundsGUI != null)
                {
                    meshBoundsGUI.DrawBoundsGizmo();
                }
            }

            SceneView.lastActiveSceneView.Repaint();

        }

        private void ToolInfoGUI()
        {
            EditorGUILayout.LabelField("VRC Avatar Editor", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.versionLabel, TOOL_VERSION);

            EditorGUILayout.Space();

            GatoGUILayout.Button(
                LocalizeText.instance.langPair.openOnlineManualButtonText,
                () => {
                    Application.OpenURL(MANUAL_URL);
                });

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(LocalizeText.instance.langPair.functionsLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                foreach (var toolFunc in TOOL_FUNCS)
                {
                    EditorGUILayout.LabelField(toolFunc);
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Created by gatosyocora");
            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Twitter", "@" + TWITTER_ID, GUILayout.Width(300));
                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.open,
                        () => {
                            Application.OpenURL("https://twitter.com/" + TWITTER_ID);
                        },
                        true,
                        GUILayout.Width(50));

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.LabelField("Discord", DISCORD_ID);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Booth", BOOTH_URL, GUILayout.Width(300));
                    GatoGUILayout.Button(
                        LocalizeText.instance.langPair.open,
                        () => {
                            Application.OpenURL(BOOTH_ITEM_URL);
                        },
                        true,
                        GUILayout.Width(50));

                    GUILayout.FlexibleSpace();
                }
            }

            EditorGUILayout.Space();

            isShowingReadme = EditorGUILayout.Foldout(isShowingReadme, LocalizeText.instance.langPair.readmeLabel);

            if (isShowingReadme)
            {
                readmeScrollPos = EditorGUILayout.BeginScrollView(readmeScrollPos, GUI.skin.box);
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.SelectableLabel(readmeText, GUILayout.Height(500));
                    }
                }
                EditorGUILayout.EndScrollView();
            }


            isShowingLicense = EditorGUILayout.Foldout(isShowingLicense, LocalizeText.instance.langPair.licenseLabel);

            if (isShowingLicense)
            {
                licenseScrollPos = EditorGUILayout.BeginScrollView(licenseScrollPos, GUI.skin.box);
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.SelectableLabel(licenseText, GUILayout.Height(500));
                    }
                }
                EditorGUILayout.EndScrollView();
            }

            isShowingUsingSoftwareLicense = EditorGUILayout.Foldout(isShowingUsingSoftwareLicense, LocalizeText.instance.langPair.usingSoftwareLicenseLabel);

            if (isShowingUsingSoftwareLicense)
            {
                usingSoftwareLicenseScrollPos = EditorGUILayout.BeginScrollView(usingSoftwareLicenseScrollPos, GUI.skin.box);
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.SelectableLabel(usingSoftwareLicenseText, GUILayout.Height(500));
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void SettingGUI()
        {

            EditorGUILayout.HelpBox(LocalizeText.instance.langPair.settingPageMessageText, MessageType.Info);

            avatarMonitorGUI.DrawSettingsGUI();

            EditorGUILayout.Space();

            faceEmotionGUI.DrawSettingsGUI();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(LocalizeText.instance.langPair.otherLabel, EditorStyles.boldLabel);

            layoutType = (LayoutType)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.layoutTypeLabel, layoutType);

            var languagePacks = LocalizeText.instance.langs;
            var index = Array.IndexOf(languagePacks, language);
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                index = EditorGUILayout.Popup(LocalizeText.instance.langPair.languageLabel, index, languagePacks);
                if (check.changed)
                {
                    language = languagePacks[index];
                    // TODO: 失敗している場合はBaseが入っているので表示もそうしないといけない
                    _ = LocalizeText.instance.LoadLanguage(language);
                    Repaint();
                }
            }

            EditorGUILayout.Space();

            GatoGUILayout.Button(
                LocalizeText.instance.langPair.saveSettingButtonText,
                () => {
                    EditorSetting.instance.SaveSettingDataToScriptableObject(
                                            layoutType, language,
                                            avatarMonitorGUI, faceEmotionGUI);
                });

            GatoGUILayout.Button(
                LocalizeText.instance.langPair.changeDefaultSettingButtonText,
                () => {
                    EditorSetting.instance.DeleteMySettingData();
                    (layoutType, language) = EditorSetting.instance.LoadSettingDataFromScriptableObject(
                                                editorFolderPath, language,
                                                avatarMonitorGUI, faceEmotionGUI);
                });
        }

        private void DrawToolSwitchTab()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    GUILayout.FlexibleSpace();
                    // タブを描画する
                    currentTool = (ToolFunc)GUILayout.Toolbar((int)currentTool, LocalizeText.instance.toolTabTexts, "LargeButton", GUI.ToolbarButtonSize.Fixed);
                    GUILayout.FlexibleSpace();

                    if (check.changed)
                    {
                        OnTabChanged();
                    }
                }
            }
        }

        public void OnTabChanged()
        {
            selectedToolGUI = toolGUIs[currentTool];

            if (currentTool == ToolFunc.FaceEmotion)
            {
                faceEmotionGUI.Initialize(edittingAvatar, originalAvatar, saveFolder, this, animationsGUI);

                if (edittingAvatar.SkinnedMeshList != null)
                {
                    for (int i = 0; i < edittingAvatar.SkinnedMeshList.Count; i++)
                    {
                        if (edittingAvatar.LipSyncShapeKeyNames != null && edittingAvatar.LipSyncShapeKeyNames.Count > 0)
                            edittingAvatar.SkinnedMeshList[i].SetExclusionBlendShapesByContains(faceEmotionGUI.blendshapeExclusions.Union(edittingAvatar.LipSyncShapeKeyNames).ToList<string>());
                    }
                }

                avatarMonitorGUI.MoveAvatarCam(true);
            }
            else
            {
                avatarMonitorGUI.MoveAvatarCam(false);
            }

            if (currentTool == ToolFunc.Shader)
            {
                shaderGUI.Initialize(edittingAvatar, originalAvatar);
            }
        }

        private void OnChangedAvatar()
        {
#if VRC_SDK_VRCSDK2
            edittingAvatar = avatarMonitorGUI.SetAvatarPreview(targetAvatarDescriptor);
            originalAvatar = new VRCAvatar(targetAvatarDescriptor);
#endif
            EditorSetting.instance.ApplySettingsToEditorGUI(edittingAvatar, faceEmotionGUI);

#if VRC_SDK_VRCSDK2
            var targetAvatarObj = targetAvatarDescriptor.gameObject;
            targetAvatarObj.SetActive(true);
#endif

            avatarMonitorGUI.MoveAvatarCam(false);
            animationsGUI.Initialize(edittingAvatar, originalAvatar, saveFolder, this, faceEmotionGUI);
            avatarInfoGUI.Initialize(originalAvatar);
            meshBoundsGUI.Initialize(originalAvatar);
            probeAnchorGUI.Initialize(originalAvatar);

            currentTool = ToolFunc.AvatarInfo;
        }

        public void OpenSubWindow()
        {
            GetWindow<AnimationLoaderGUI>("Animation Loader", typeof(VRCAvatarEditorGUI));
        }

        [MenuItem("VRCAvatarEditor/Check for Updates")]
        public static async void CheckForUpdates()
        {
            var remoteVersion = await VersionCheckUtility.GetLatestVersionFromRemote(GITHUB_LATEST_RELEASE_API_URL);
            var isLatest = VersionCheckUtility.IsLatestVersion(TOOL_VERSION, remoteVersion);
            var message = (isLatest) ? 
                            LocalizeText.instance.langPair.localIsLatestMessageText.Replace("<LocalVersion>", TOOL_VERSION) :
                            LocalizeText.instance.langPair.localIsLatestMessageText.Replace("<LocalVersion>", TOOL_VERSION).Replace("<RemoteVersion>", remoteVersion);
            var okText = (isLatest) ? 
                            LocalizeText.instance.langPair.ok :
                            LocalizeText.instance.langPair.downloadLatestButtonText;
            if (EditorUtility.DisplayDialog(LocalizeText.instance.langPair.checkVersionDialogTitle, message, okText) && !isLatest) 
            {
                Application.OpenURL(BOOTH_ITEM_URL);
            }
        }

        // TODO: NowLoadingをもう少しいい感じにする
        private void DrawNowLoading()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    var style = new GUIStyle(GUI.skin.label)
                    {
                        wordWrap = true
                    };
                    EditorGUILayout.LabelField("Now Loading...", style);
                    GUILayout.FlexibleSpace();
                }
                GUILayout.FlexibleSpace();
            }
        }
    }
}