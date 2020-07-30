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
using VRCSDK2;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{
    public class VRCAvatarEditorGUI : EditorWindow
    {
        private const string TOOL_VERSION = "v0.5";
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

        private bool newSDKUI;
        private bool needRepaint = false;

        private VRC_AvatarDescriptor targetAvatarDescriptor;
        private VRCAvatarEditor.Avatar edittingAvatar = null;
        private VRCAvatarEditor.Avatar originalAvatar = null;

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

        public ToolFunc currentTool = ToolFunc.AvatarInfo;

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
            edittingAvatar = new Avatar();

            var editorScriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            editorFolderPath = Path.GetDirectoryName(editorScriptPath);
            editorFolderPath = editorFolderPath.Substring(0, editorFolderPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);

            saveFolder = "Assets/";


            licenseText = GetFileTexts(editorFolderPath + LICENSE_FILE_NAME);
            readmeText = GetFileTexts(editorFolderPath + README_FILE_NAME);
            usingSoftwareLicenseText = GetFileTexts(editorFolderPath + USING_SOFTWARE_FILE_NAME);

            avatarMonitorGUI = ScriptableObject.CreateInstance<AvatarMonitorGUI>();
            animationsGUI = ScriptableObject.CreateInstance<AnimationsGUI>();
            avatarInfoGUI = ScriptableObject.CreateInstance<AvatarInfoGUI>();
            faceEmotionGUI = ScriptableObject.CreateInstance<FaceEmotionGUI>();
            probeAnchorGUI = ScriptableObject.CreateInstance<ProbeAnchorGUI>();
            meshBoundsGUI = ScriptableObject.CreateInstance<MeshBoundsGUI>();
            shaderGUI = ScriptableObject.CreateInstance<ShaderGUI>();

            avatarMonitorGUI.Initialize(currentTool);
            animationsGUI.Initialize(ref edittingAvatar, originalAvatar, saveFolder, this, faceEmotionGUI);
            avatarInfoGUI.Initialize(ref originalAvatar);
            probeAnchorGUI.Initialize(ref originalAvatar);

            LoadSettingDataFromScriptableObject();

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

            newSDKUI = IsNewSDKUI();

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

                using (new EditorGUI.DisabledGroupScope(originalAvatar is null))
                {
                    if (GUILayout.Button(LocalizeText.instance.langPair.reloadAvatarButtonText))
                    {
                        OnChangedAvatar();
                    }
                }

                EditorGUILayout.Space();

                var toolInfoButtonText = (!isShowingToolInfo) ? LocalizeText.instance.langPair.toolInfoButtonText : LocalizeText.instance.langPair.close;
                var settingButtonText = (!isShowingSetting) ? LocalizeText.instance.langPair.settingButtonText : LocalizeText.instance.langPair.close;
                if (GUILayout.Button(toolInfoButtonText, GUILayout.MinWidth(50)))
                {
                    isShowingToolInfo = !isShowingToolInfo;
                    isShowingSetting = false;
                }

                if (GUILayout.Button(settingButtonText, GUILayout.MinWidth(50)))
                {
                    isShowingSetting = !isShowingSetting;
                    isShowingToolInfo = false;

                    if (!isShowingSetting)
                        ApplySettingsToEditorGUI();
                }
            }

            if (!isShowingToolInfo && !isShowingSetting)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    // アバター選択
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        targetAvatarDescriptor = EditorGUILayout.ObjectField(
                            LocalizeText.instance.langPair.avatarLabel,
                            targetAvatarDescriptor,
                            typeof(VRC_AvatarDescriptor),
                            allowSceneObjects: true
                        ) as VRC_AvatarDescriptor;

                        if (check.changed)
                        {
                            // アバター変更時の処理
                            if (targetAvatarDescriptor != null)
                            {
                                OnChangedAvatar();
                            }
                        }
                    }

                    using (new EditorGUI.DisabledGroupScope(edittingAvatar.descriptor == null))
                    {
                        // LayoutType: Default
                        if (layoutType == LayoutType.Default)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                needRepaint = avatarMonitorGUI.DrawGUI(null);
                                if (needRepaint) Repaint();

                                if (!needRepaint)
                                    animationsGUI.DrawGUI(layoutOptions[0]);
                            }

                            // 各種機能
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
                                        TabChanged();
                                    }
                                }
                            }

                            if (currentTool == ToolFunc.AvatarInfo)
                            {
                                // アバター情報
                                avatarInfoGUI.DrawGUI(null);
                            }
                            else if (currentTool == ToolFunc.FaceEmotion)
                            {
                                // 表情設定
                                faceEmotionGUI.DrawGUI(null);
                            }
                            else if (currentTool == ToolFunc.ProbeAnchor)
                            {
                                // Probe Anchor設定
                                probeAnchorGUI.DrawGUI(null);
                            }
                            else if (currentTool == ToolFunc.Bounds)
                            {
                                // Bounds設定
                                meshBoundsGUI.DrawGUI(null);
                            }
                            else if (currentTool == ToolFunc.Shader)
                            {
                                // Shader設定
                                shaderGUI.DrawGUI(null);
                            }
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
                                    // 各種機能
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        using (var check = new EditorGUI.ChangeCheckScope())
                                        {
                                            // タブを描画する
                                            currentTool = (ToolFunc)GUILayout.Toolbar((int)currentTool, LocalizeText.instance.toolTabTexts, "LargeButton", GUI.ToolbarButtonSize.Fixed);

                                            if (check.changed)
                                            {
                                                TabChanged();
                                            }
                                        }
                                    }

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
                                    else if (currentTool == ToolFunc.FaceEmotion)
                                    {
                                        // 表情設定
                                        faceEmotionGUI.DrawGUI(null);
                                    }
                                    else if (currentTool == ToolFunc.ProbeAnchor)
                                    {
                                        // Probe Anchor設定
                                        probeAnchorGUI.DrawGUI(null);
                                    }
                                    else if (currentTool == ToolFunc.Bounds)
                                    {
                                        // Bounds設定
                                        meshBoundsGUI.DrawGUI(null);
                                    }
                                    else if (currentTool == ToolFunc.Shader)
                                    {
                                        // Shader設定
                                        shaderGUI.DrawGUI(null);
                                    }
                                }
                            }
                        }

                        EditorGUILayout.Space();

                        // ポーズ修正
                        if (GUILayout.Button(LocalizeText.instance.langPair.resetPoseButtonText))
                        {
                            HumanoidPose.ResetPose(edittingAvatar.descriptor.gameObject);
                            HumanoidPose.ResetPose(originalAvatar.descriptor.gameObject);
                        }

                        // アップロード
                        if (GUILayout.Button(LocalizeText.instance.langPair.uploadAvatarButtonText))
                        {
                            UploadAvatar(newSDKUI);
                        }
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

            if (GUILayout.Button(LocalizeText.instance.langPair.openOnlineManualButtonText))
                Application.OpenURL(MANUAL_URL);

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
                    if (GUILayout.Button(LocalizeText.instance.langPair.open, GUILayout.Width(50)))
                        Application.OpenURL("https://twitter.com/" + TWITTER_ID);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.LabelField("Discord", DISCORD_ID);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Booth", BOOTH_URL, GUILayout.Width(300));
                    if (GUILayout.Button(LocalizeText.instance.langPair.open, GUILayout.Width(50)))
                        Application.OpenURL(BOOTH_ITEM_URL);
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

            var languagePacks = GetLanguagePacks();
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

            if (GUILayout.Button(LocalizeText.instance.langPair.saveSettingButtonText))
            {
                SaveSettingDataToScriptableObject();
            }
            if (GUILayout.Button(LocalizeText.instance.langPair.changeDefaultSettingButtonText))
            {
                DeleteMySettingData();
                LoadSettingDataFromScriptableObject();
            }

        }

        public void TabChanged()
        {
            if (currentTool == ToolFunc.FaceEmotion)
            {
                faceEmotionGUI.Initialize(ref edittingAvatar, originalAvatar, saveFolder, this, animationsGUI);

                if (edittingAvatar.skinnedMeshList != null)
                {
                    for (int i = 0; i < edittingAvatar.skinnedMeshList.Count; i++)
                    {
                        if (edittingAvatar.lipSyncShapeKeyNames != null && edittingAvatar.lipSyncShapeKeyNames.Count > 0)
                            edittingAvatar.skinnedMeshList[i].SetExclusionBlendShapesByContains(faceEmotionGUI.blendshapeExclusions.Union(edittingAvatar.lipSyncShapeKeyNames).ToList<string>());
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
                shaderGUI.Initialize(ref edittingAvatar, originalAvatar);
            }
        }

        private void OnChangedAvatar()
        {
            edittingAvatar = avatarMonitorGUI.SetAvatarPreview(targetAvatarDescriptor);
            originalAvatar = new Avatar(targetAvatarDescriptor);
            ApplySettingsToEditorGUI();

            var targetAvatarObj = targetAvatarDescriptor.gameObject;
            targetAvatarObj.SetActive(true);

            avatarMonitorGUI.MoveAvatarCam(false);
            animationsGUI.Initialize(ref edittingAvatar, originalAvatar, saveFolder, this, faceEmotionGUI);
            avatarInfoGUI.Initialize(ref originalAvatar);
            meshBoundsGUI.Initialize(ref originalAvatar);
            probeAnchorGUI.Initialize(ref originalAvatar);

            currentTool = ToolFunc.AvatarInfo;
        }

        private string[] GetLanguagePacks()
        {
            return LocalizeText.instance.langs;
        }

        #region General Functions

        /// <summary>
        /// 設定情報を読み込む
        /// </summary>
        private void LoadSettingDataFromScriptableObject()
        {
            LocalizeText.instance.LoadLanguageTypesFromLocal(editorFolderPath);
            if (string.IsNullOrEmpty(language) || EditorSetting.instance.Data.language != LocalizeText.instance.langPair.name)
            {
                // awaitするとUIスレッドが止まっておかしくなるのでawaitしない
                _ = LocalizeText.instance.LoadLanguage(EditorSetting.instance.Data.language);
            }

            layoutType = EditorSetting.instance.Data.layoutType;
            language = EditorSetting.instance.Data.language;

            avatarMonitorGUI.LoadSettingData(EditorSetting.instance.Data);
            faceEmotionGUI.LoadSettingData(EditorSetting.instance.Data);
        }

        /// <summary>
        /// 設定情報を保存する
        /// </summary>
        private void SaveSettingDataToScriptableObject()
        {
            bool newCreated = false;
            var settingAsset = Resources.Load<SettingData>("CustomSettingData");

            if (settingAsset == null)
            {
                settingAsset = CreateInstance<SettingData>();
                newCreated = true;
            }

            avatarMonitorGUI.SaveSettingData(ref settingAsset);

            faceEmotionGUI.SaveSettingData(ref settingAsset);

            settingAsset.layoutType = layoutType;
            settingAsset.language = language;

            if (newCreated)
            {
                var data = Resources.Load<SettingData>("DefaultSettingData");
                var resourceFolderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(data)) + "/";
                AssetDatabase.CreateAsset(settingAsset, resourceFolderPath + "CustomSettingData.asset");
                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.SetDirty(settingAsset);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 自分の設定情報を削除する
        /// </summary>
        private void DeleteMySettingData()
        {
            // 一度読み込んでみて存在するか確認
            var settingAsset = Resources.Load<SettingData>("CustomSettingData");
            if (settingAsset == null) return;

            AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(settingAsset.GetInstanceID()));
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 設定を反映する
        /// </summary>
        private void ApplySettingsToEditorGUI()
        {
            if (edittingAvatar.descriptor == null) return;

            foreach (var skinnedMesh in edittingAvatar.skinnedMeshList)
            {
                if (edittingAvatar.lipSyncShapeKeyNames != null && edittingAvatar.lipSyncShapeKeyNames.Count > 0)
                    skinnedMesh.SetExclusionBlendShapesByContains(faceEmotionGUI.blendshapeExclusions.Union(edittingAvatar.lipSyncShapeKeyNames).ToList<string>());

                if (faceEmotionGUI.selectedSortType == FaceEmotionGUI.SortType.AToZ)
                    skinnedMesh.SortBlendShapesToAscending();
                else
                    skinnedMesh.ResetDefaultSort();
            }
        }

        /// <summary>
        /// pathのファイル内容を取得する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetFileTexts(string path)
        {
            string text = string.Empty;
            var fi = new FileInfo(path);
            try
            {
                using (StreamReader sr = new StreamReader(fi.OpenRead(), Encoding.UTF8))
                {
                    text += sr.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                // 改行コード
                text += "読み込みに失敗しました:" + e.Message;
            }

            return text;
        }

        /// <summary>
        /// VRCSDKのバージョンを取得する
        /// </summary>
        /// <returns></returns>
        private string GetVRCSDKVersion()
        {
            string path = GetVRCSDKFilePath("version");
            return GetFileTexts(path);
        }

        /// <summary>
        /// VRCSDKに含まれるファイルを取得する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetVRCSDKFilePath(string fileName)
        {
            // VRCSDKフォルダが移動されている可能性があるため対象ファイルを探す
            var guids = AssetDatabase.FindAssets(fileName, null);
            string path = string.Empty;
            bool couldFindFile = false;
            foreach (var guid in guids)
            {
                path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("VRCSDK/"))
                {
                    couldFindFile = true;
                    break;
                }
            }
            if (couldFindFile)
                return path;
            else
                return string.Empty;
        }

        /// <summary>
        /// VRCSDKが新しいUIかどうか
        /// </summary>
        /// <returns></returns>
        private bool IsNewSDKUI()
        {
            var sdkVersion = GetVRCSDKVersion();
            // 新UI以降のバージョンにはファイルが存在するため何かしらは返ってくる
            if (string.IsNullOrEmpty(sdkVersion)) return false;

            var dotChar = '.';
            var zero = '0';
            var versions = sdkVersion.Split(dotChar);
            var version =
                    versions[0].PadLeft(4, zero) + dotChar +
                    versions[1].PadLeft(2, zero) + dotChar +
                    versions[2].PadLeft(2, zero);
            var newVersion = "2019.08.23";

            return newVersion.CompareTo(version) <= 0;
        }

        private void UploadAvatar(bool newSDKUI)
        {
            if (newSDKUI)
            {
                EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
            }
            else
            {
                EditorApplication.ExecuteMenuItem("VRChat SDK/Show Build Control Panel");
            }
        }

        public void OpenSubWindow()
        {
            GetWindow<AnimationLoaderGUI>("Animation Loader", typeof(VRCAvatarEditorGUI));
        }

        #endregion

        [MenuItem("VRCAvatarEditor/Check for Updates")]
        public static async void CheckForUpdates()
        {
            var latestVersion = await GetLatestVersionFromRemote();
            var isLatest = IsLatestVersion(TOOL_VERSION, latestVersion);
            var message = (isLatest) ? $"VRCAvatarEditor {TOOL_VERSION} は最新です" : $"最新バージョンがあります(現在: {TOOL_VERSION}, 最新: {latestVersion})";
            var okText = (isLatest) ? "OK" : "ダウンロードする";
            if (EditorUtility.DisplayDialog("バージョン確認", message, okText) && !isLatest) 
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

        private static async Task<string> GetLatestVersionFromRemote()
        {
            var request = UnityWebRequest.Get(GITHUB_LATEST_RELEASE_API_URL);
            await request.SendWebRequest();

            if (request.isNetworkError || request.isHttpError)
            {
                Debug.LogError(request.error);
                return string.Empty;
            }
            else
            {
                var jsonData = request.downloadHandler.text;
                return JsonUtility.FromJson<GitHubData>(jsonData)?.tag_name ?? string.Empty;
            }
        }

        private static bool IsLatestVersion(string local, string remote)
        {
            var localVersion = local.Substring(1).Split('.').Select(x => int.Parse(x)).ToArray();
            var remoteVersion = remote.Substring(1).Split('.').Select(x => int.Parse(x)).ToArray();
            
            // サイズを合わせる
            if (localVersion.Length < remoteVersion.Length)
            {
                localVersion = Enumerable.Range(0, remoteVersion.Length)
                                    .Select(i =>
                                    {
                                        if (i < localVersion.Length) return localVersion[i];
                                        else return 0;
                                    })
                                    .ToArray();
            }
            else if (localVersion.Length > remoteVersion.Length)
            {
                remoteVersion = Enumerable.Range(0, localVersion.Length)
                                    .Select(i =>
                                    {
                                        if (i < remoteVersion.Length) return remoteVersion[i];
                                        else return 0;
                                    })
                                    .ToArray();
            }

            for (int index = 0; index < localVersion.Length; index++)
            {
                var l = localVersion[index];
                var r = remoteVersion[index];
                if (l < r) return false;
                if (l > r) return true;
            }
            return true;
        }
    }

    public class GitHubData
    {
        public string tag_name;
    }
}