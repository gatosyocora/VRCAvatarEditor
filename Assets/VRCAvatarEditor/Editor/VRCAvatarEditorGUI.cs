﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRCSDK2;
using VRC.Core;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System;
using VRCAvatarEditor;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{
    public class VRCAvatarEditorGUI : EditorWindow
    {
        private const string TOOL_VERSION = "beta v0.2.3";
        private const string TWITTER_ID = "gatosyocora";
        private const string DISCORD_ID = "gatosyocora#9575";
        private const string MANUAL_URL = "https://docs.google.com/document/d/1DU7mP5PTvERqHzZiiCBJ9ep5CilQ1iaXC_3IoiuPEgA/edit?usp=sharing";

        private AvatarMonitorGUI avatarMonitorGUI;
        public AnimationsGUI animationsGUI;
        private AvatarInfoGUI avatarInfoGUI;
        private FaceEmotionGUI faceEmotionGUI;
        private ProbeAnchorGUI probeAnchorGUI;

        private bool newSDKUI;
        private bool needRepaint = false;
        
        private VRCAvatarEditor.Avatar edittingAvatar = null;

        private string editorFolderPath;

        private string saveFolder;

        public enum ToolFunc
        {
            アバター情報,
            表情設定,
            ProbeAnchor,
            Bounds,
            Shader,
        }

        private ToolFunc currentTool = ToolFunc.アバター情報;

        private static class ToolTab
        {
            private static GUIContent[] _tabToggles = null;
            public static GUIContent[] TabToggles
            {
                get
                {
                    if (_tabToggles == null)
                    {
                        _tabToggles = System.Enum.GetNames(typeof(ToolFunc)).Select(x => new GUIContent(x)).ToArray();
                    }
                    return _tabToggles;
                }
            }

            public static readonly GUIStyle TabButtonStyle = "LargeButton";

            public static readonly GUI.ToolbarButtonSize TabButtonSize = GUI.ToolbarButtonSize.Fixed;
        }

        private GUILayoutOption[][] layoutOptions
                        = new GUILayoutOption[][] 
                            {
                                new GUILayoutOption[]{ GUILayout.MinWidth(300), GUILayout.MaxHeight(270) },
                                new GUILayoutOption[]{ GUILayout.Height(200)}
                            };

        #region MeshBounds Variable
        private List<SkinnedMeshRenderer> targetRenderers;
        private List<SkinnedMeshRenderer> exclusions = new List<SkinnedMeshRenderer>();
        #endregion

        #region Shader Variable

        private Vector2 leftScrollPosShader = Vector2.zero;

        private static class ShaderUI
        {
            private static bool isOpening = false;

            public static bool Shader(float btnWidth, float btnHeight)
            {
                var windowRect = GUILayoutUtility.GetLastRect();

                if (btnHeight == 0f) btnHeight = windowRect.height;

                Vector2 btnPos;

                if (isOpening)
                {
                    var shaderRect = windowRect;
                    shaderRect.width = shaderRect.width / 2f - btnHeight;
                    shaderRect.position = new Vector2(windowRect.width / 2f + btnHeight*2, shaderRect.position.y);
                    EditorGUI.DrawRect(shaderRect, new Color(0.9f, 0.9f, 0.9f));
                    
                    btnPos = new Vector2(windowRect.width / 2f, windowRect.position.y + btnHeight);
                }
                else
                    btnPos = new Vector2(windowRect.width - btnHeight*2, windowRect.position.y + btnHeight);

                var btnRect = new Rect(btnPos.x, btnPos.y, btnWidth, btnHeight);

                GUIUtility.RotateAroundPivot(-90f, btnRect.center);

                if (GUI.Button(btnRect, "test"))
                    isOpening = !isOpening;

                GUI.matrix = Matrix4x4.identity;

                return isOpening;
            }
        }

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
        private bool isShowingReadme = false;
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

        private bool isActiveOnlySelectedAvatar = true;
        private LayoutType layoutType = LayoutType.Default;

        #endregion


        [MenuItem("VRCAvatarEditor/Editor")]
        private static void Create()
        {
            var window = GetWindow<VRCAvatarEditorGUI>("VRCAvatarEditor");
            window.minSize = new Vector2(650f, 500f);
        }

        private void OnEnable()
        {
            edittingAvatar = new Avatar();
            
            saveFolder = editorFolderPath + "Animations/";
            
            avatarMonitorGUI = new AvatarMonitorGUI(ref edittingAvatar, currentTool);
            animationsGUI = new AnimationsGUI(ref edittingAvatar, saveFolder);
            avatarInfoGUI = new AvatarInfoGUI(ref edittingAvatar);
            faceEmotionGUI = new FaceEmotionGUI(ref edittingAvatar, saveFolder, this);
            probeAnchorGUI = new ProbeAnchorGUI(ref edittingAvatar);

            var editorScriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            editorFolderPath = Path.GetDirectoryName(editorScriptPath).Replace("Editor/", string.Empty) + "/";
            
            licenseText = GetFileTexts(editorFolderPath + LICENSE_FILE_NAME);
            readmeText = GetFileTexts(editorFolderPath + README_FILE_NAME);
            usingSoftwareLicenseText = GetFileTexts(editorFolderPath + USING_SOFTWARE_FILE_NAME);
            
            LoadSettingDataFromScriptableObject();

            // Windowを開いたときにオブジェクトが選択されていればそれをアバターとして設定する
            if (Selection.gameObjects.Length == 1)
            {
                var descriptor = Selection.gameObjects[0].GetComponent<VRC_AvatarDescriptor>();
                if (descriptor != null)
                {
                    edittingAvatar.descriptor = descriptor;
                    
                    SetAvatarActive(edittingAvatar.descriptor);
                    edittingAvatar.LoadAvatarInfo();
                    probeAnchorGUI.SettingForProbeSetter();
                    ApplySettingsToEditorGUI();
                    avatarMonitorGUI.SetAvatarCam(edittingAvatar.descriptor.gameObject);
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

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("ToolInfo", GUILayout.MinWidth(50)))
                {
                    isShowingToolInfo = !isShowingToolInfo;
                    isShowingSetting = false;
                }

                if (GUILayout.Button("Setting", GUILayout.MinWidth(50)))
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
                        edittingAvatar.descriptor = EditorGUILayout.ObjectField(
                            "Avatar",
                            edittingAvatar.descriptor,
                            typeof(VRC_AvatarDescriptor),
                            true
                        ) as VRC_AvatarDescriptor;

                        if (check.changed)
                        {
                            // アバター変更時の処理
                            if (edittingAvatar.descriptor != null)
                            {
                                targetRenderers = null;

                                SetAvatarActive(edittingAvatar.descriptor);
                                edittingAvatar.LoadAvatarInfo();
                                probeAnchorGUI.SettingForProbeSetter();
                                ApplySettingsToEditorGUI();
                                avatarMonitorGUI.SetAvatarCam(edittingAvatar.descriptor.gameObject);
                            }
                        }
                    }

                    using (new EditorGUI.DisabledGroupScope(edittingAvatar.descriptor == null))
                    {
                        // LayoutType: Defalut
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
                                    currentTool = (ToolFunc)GUILayout.Toolbar((int)currentTool, ToolTab.TabToggles, ToolTab.TabButtonStyle, ToolTab.TabButtonSize);
                                    GUILayout.FlexibleSpace();

                                    if (check.changed)
                                    {
                                        TabChanged();
                                    }
                                }
                            }

                            if (currentTool == ToolFunc.アバター情報)
                            {
                                // アバター情報
                                avatarInfoGUI.DrawGUI(null);
                            }
                            else if (currentTool == ToolFunc.表情設定)
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
                                MeshBoundsGUI();
                            }
                            else if (currentTool == ToolFunc.Shader)
                            {
                                // Shader設定
                                ShaderGUI();
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
                                            currentTool = (ToolFunc)GUILayout.Toolbar((int)currentTool, ToolTab.TabToggles, ToolTab.TabButtonStyle, ToolTab.TabButtonSize);

                                            if (check.changed)
                                            {
                                                TabChanged();
                                            }
                                        }
                                    }

                                    if (currentTool == ToolFunc.アバター情報)
                                    {
                                        using (new EditorGUILayout.HorizontalScope())
                                        {
                                            if (!needRepaint)
                                                animationsGUI.DrawGUI(layoutOptions[1]);
                                        }

                                        // アバター情報
                                        avatarInfoGUI.DrawGUI(null);

                                    }
                                    else if (currentTool == ToolFunc.表情設定)
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
                                        MeshBoundsGUI();
                                    }
                                    else if (currentTool == ToolFunc.Shader)
                                    {
                                        // Shader設定
                                        ShaderGUI();
                                    }
                                }
                            }
                        }

                        EditorGUILayout.Space();

                        // ポーズ修正
                        if (GUILayout.Button("Reset Pose"))
                        {
                            HumanoidPose.ResetPose(edittingAvatar.descriptor.gameObject);
                        }

                        // アップロード
                        if (GUILayout.Button("Upload Avatar"))
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
                foreach (var renderer in targetRenderers)
                {
                    MeshBounds.DrawBoundsGizmo(renderer);
                }
            }

            SceneView.lastActiveSceneView.Repaint();

        }

        // TODO: UIの見直し
        private void MeshBoundsGUI()
        {
            if (targetRenderers == null && edittingAvatar != null)
            {
                targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                    edittingAvatar.descriptor.gameObject,
                                    exclusions);
            }

            EditorGUILayout.LabelField("Bounds", EditorStyles.boldLabel);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Reset Bounds to Prefab"))
                {
                    MeshBounds.RevertBoundsToPrefab(targetRenderers);
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                EditorGUILayout.LabelField("Exclusions");

                using (new EditorGUI.IndentLevelScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        var parentObject = EditorGUILayout.ObjectField(
                            "Child objects",
                            null,
                            typeof(GameObject),
                            true
                        ) as GameObject;

                        if (check.changed && parentObject != null && edittingAvatar != null)
                        {
                            var renderers = parentObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                            foreach (var renderer in renderers)
                            {
                                exclusions.Add(renderer);
                            }
                            exclusions = exclusions.Distinct().ToList();

                            targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                                            edittingAvatar.descriptor.gameObject,
                                                            exclusions);
                        }
                    }

                    EditorGUILayout.Space();

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button("+", GUILayout.MaxWidth(60)))
                        {
                            exclusions.Add(null);
                        }
                    }

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {

                        for (int i = 0; i < exclusions.Count; i++)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                exclusions[i] = EditorGUILayout.ObjectField(
                                    "Object " + (i + 1),
                                    exclusions[i],
                                    typeof(SkinnedMeshRenderer),
                                    true
                                ) as SkinnedMeshRenderer;

                                if (GUILayout.Button("x", GUILayout.MaxWidth(30)))
                                {
                                    exclusions.RemoveAt(i);
                                }
                            }
                        }

                        if (check.changed && edittingAvatar != null)
                        {
                            targetRenderers = MeshBounds.GetSkinnedMeshRenderersWithoutExclusions(
                                                edittingAvatar.descriptor.gameObject,
                                                exclusions);
                        }
                    }
                    
                    EditorGUILayout.Space();
                }
            }

            if (GUILayout.Button("Set Bounds"))
            {
                MeshBounds.BoundsSetter(targetRenderers);
            }
        }

        private void ShaderGUI()
        {
            EditorGUILayout.LabelField("Shader", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope())
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScrollPosShader))
                {
                    leftScrollPosShader = scrollView.scrollPosition;
                    using (new EditorGUI.IndentLevelScope())
                    {
                        if (edittingAvatar.materials != null)
                        {
                            foreach (var mat in edittingAvatar.materials)
                            {
                                if (mat == null) continue;
                                if (mat.shader == null) continue;

                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField(mat.shader.name);
                                    EditorGUILayout.LabelField("("+mat.name+")");
                                    if (GUILayout.Button("Select"))
                                    {
                                        Selection.activeObject = mat;
                                    }
                                }

                            }
                        }
                    }
                }
            }
        }

        private void ToolInfoGUI()
        {
            EditorGUILayout.LabelField("VRC Avatar Editor",EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Version", TOOL_VERSION);

            EditorGUILayout.Space();

            if (GUILayout.Button("オンラインマニュアル"))
                Application.OpenURL(MANUAL_URL);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Functions");
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
                    EditorGUILayout.LabelField("Twitter", "@"+TWITTER_ID, GUILayout.Width(300));
                    if (GUILayout.Button("Open", GUILayout.Width(50)))
                        Application.OpenURL("https://twitter.com/"+ TWITTER_ID);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.LabelField("Discord", DISCORD_ID);
            }

            EditorGUILayout.Space();
            
            isShowingReadme = EditorGUILayout.Foldout(isShowingReadme, "This Tool's Readme");

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


            isShowingLicense =  EditorGUILayout.Foldout(isShowingLicense, "This Tool's License");

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

            isShowingUsingSoftwareLicense = EditorGUILayout.Foldout(isShowingUsingSoftwareLicense, "Using Software License");

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

            EditorGUILayout.HelpBox("設定は変更後からウィンドウを閉じるまで適用されます。「Save Setting」で次回以降も適用されます", MessageType.Info);

            avatarMonitorGUI.DrawSettingsGUI();

            EditorGUILayout.Space();

            faceEmotionGUI.DrawSettingsGUI();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Other", EditorStyles.boldLabel);
            isActiveOnlySelectedAvatar = EditorGUILayout.ToggleLeft("選択中のアバターだけActiveにする", isActiveOnlySelectedAvatar);

            layoutType = (LayoutType)EditorGUILayout.EnumPopup("レイアウト", layoutType);

            if (GUILayout.Button("Save Setting"))
            {
                SaveSettingDataToScriptableObject();
            }
            if (GUILayout.Button("Default Setting"))
            {
                DeleteMySettingData();
                LoadSettingDataFromScriptableObject();
            }

        }

        private void TabChanged()
        {
            avatarMonitorGUI.ChangeTab(currentTool);
            avatarMonitorGUI.MoveAvatarCam();

            if (currentTool == ToolFunc.表情設定)
            {
                if (edittingAvatar.skinnedMeshList != null)
                {
                    for (int i = 0; i < edittingAvatar.skinnedMeshList.Count; i++)
                    {
                        if (edittingAvatar.lipSyncShapeKeyNames != null && edittingAvatar.lipSyncShapeKeyNames.Count > 0)
                            edittingAvatar.skinnedMeshList[i].SetExclusionBlendShapesByContains(faceEmotionGUI.blendshapeExclusions.Union(edittingAvatar.lipSyncShapeKeyNames).ToList<string>());
                    }
                }
            }
        }

        #region General Functions

        /// <summary>
        /// 設定情報を読み込む
        /// </summary>
        private void LoadSettingDataFromScriptableObject()
        {
            var settingAsset = Resources.Load<SettingData>("CustomSettingData");

            if (settingAsset == null)
                settingAsset = Resources.Load<SettingData>("DefaultSettingData");
            
            isActiveOnlySelectedAvatar = settingAsset.isActiveOnlySelectedAvatar;
            layoutType = settingAsset.layoutType;

            avatarMonitorGUI.LoadSettingData(settingAsset);
            faceEmotionGUI.LoadSettingData(settingAsset);
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

            settingAsset.isActiveOnlySelectedAvatar = isActiveOnlySelectedAvatar;
            settingAsset.layoutType = layoutType;

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
        /// targetAvatarのみアクティブにする
        /// </summary>
        /// <param name="targetAvatar"></param>
        private void SetAvatarActive(VRC_AvatarDescriptor targetAvatar)
        {
            var targetObj = targetAvatar.gameObject;

            if (!isActiveOnlySelectedAvatar)
            {
                targetObj.SetActive(true);
                return;
            }

            var allAvatars = Resources.FindObjectsOfTypeAll(typeof(VRC_AvatarDescriptor)) as VRC_AvatarDescriptor[];

            foreach (var avatar in allAvatars)
                avatar.gameObject.SetActive(avatar.gameObject == targetObj);
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
                text += "読み込みに失敗しました";
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
            GetWindow<AnimationLoaderGUI>("Animation Loader", true);
        }

        #endregion
    }

}