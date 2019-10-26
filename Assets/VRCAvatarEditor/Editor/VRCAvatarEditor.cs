using System.Collections;
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
    public class VRCAvatarEditor : EditorWindow
    {
        private const string TOOL_VERSION = "beta v0.2.3";
        private const string TWITTER_ID = "gatosyocora";
        private const string DISCORD_ID = "gatosyocora#9575";
        private const string MANUAL_URL = "https://docs.google.com/document/d/1DU7mP5PTvERqHzZiiCBJ9ep5CilQ1iaXC_3IoiuPEgA/edit?usp=sharing";

        private AvatarMonitorGUI avatarMonitorGUI;

        private bool newSDKUI;
        private bool needRepaint = false;

        // Avatarの情報
        public class Avatar
        {
            public Animator animator { get; set; }
            public VRC_AvatarDescriptor descriptor { get; set; }
            public Vector3 eyePos { get; set; }
            public AnimatorOverrideController standingAnimController { get; set; }
            public AnimatorOverrideController sittingAnimController { get; set; }
            public VRC_AvatarDescriptor.AnimationSet sex { get; set; }
            public string avatarId { get; set; }
            public int overridesNum { get; set; }
            public SkinnedMeshRenderer faceMesh { get; set; }
            public List<string> lipSyncShapeKeyNames;
            public List<Material> materials { get; set; }
            public int triangleCount { get; set; }
            public int triangleCountInactive { get; set; }
            public VRC_AvatarDescriptor.LipSyncStyle lipSyncStyle { get; set; }
            public Enum faceShapeKeyEnum { get; set; }

            public Avatar()
            {
                animator = null;
                descriptor = null;
                eyePos = Vector3.zero;
                standingAnimController = null;
                sittingAnimController = null;
                sex = VRC_AvatarDescriptor.AnimationSet.None;
                avatarId = string.Empty;
                overridesNum = 0;
                faceMesh = null;
                lipSyncShapeKeyNames = null;
                triangleCount = 0;
                triangleCountInactive = 0;
                lipSyncStyle = VRC_AvatarDescriptor.LipSyncStyle.Default;
                faceShapeKeyEnum = null;
            }
            
        };
        private Avatar edittingAvatar = null;

        private string editorFolderPath;

        private enum Tab
        {
            Standing,
            Sitting,
        }

        private Tab _tab = Tab.Standing;

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

        #region Animations Variable

        private readonly string[] HANDANIMS = { "FIST", "FINGERPOINT", "ROCKNROLL", "HANDOPEN", "THUMBSUP", "VICTORY", "HANDGUN" };

        private string kind;
        string titleText;
        AnimatorOverrideController controller;

        #endregion

        #region AvatarInfo Variable

        private bool isOpeningLipSync = false;
        private Vector2 lipSyncScrollPos = Vector2.zero;
        private const int LIPSYNC_SYPEKEY_NUM = 15;

        #endregion

        #region FaceEmotion Variable

        private string animName = "faceAnim";
        private string saveFolder;
        private HandPose.HandPoseType selectedHandAnim = HandPose.HandPoseType.None;

        private List<SkinnedMesh> skinnedMeshList = null;
        private Vector2 scrollPos = Vector2.zero;

        private bool isExclusionKey;

        public enum SortType
        {
            UnSort,
            AToZ,
        }

        private SendData sendData;

        #endregion

        #region ProbeAnchor Variable

        private ProbeAnchor.TARGETPOS targetPos = ProbeAnchor.TARGETPOS.HEAD;

        private bool isGettingSkinnedMeshRenderer = true;
        private bool isGettingMeshRenderer = true;

        private bool isOpeningRendererList = false;

        private List<SkinnedMeshRenderer> skinnedMeshRendererList;
        private List<MeshRenderer> meshRendererList;

        private bool[] isSettingToSkinnedMesh = null;
        private bool[] isSettingToMesh = null;

        private Vector2 leftScrollPos = Vector2.zero;
        #endregion

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
        private bool isOpeningBlendShapeExclusionList = false;

        public enum LayoutType
        {
            Default,
            Half,
        }

        #endregion

        #region Changeable Parameters from Setting

        private SortType selectedSortType = SortType.UnSort;
        private List<string> blendshapeExclusions = new List<string> { "vrc.v_", "vrc.blink_", "vrc.lowerlid_", "vrc.owerlid_", "mmd" };

        private bool isActiveOnlySelectedAvatar = true;
        private LayoutType layoutType = LayoutType.Default;

        #endregion


        [MenuItem("VRCAvatarEditor/Editor")]
        private static void Create()
        {
            var window = GetWindow<VRCAvatarEditor>("VRCAvatarEditor");
            window.minSize = new Vector2(650f, 500f);
        }

        private void OnEnable()
        {

            edittingAvatar = new Avatar();

            avatarMonitorGUI = new AvatarMonitorGUI(ref edittingAvatar, currentTool);

            var editorScriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            editorFolderPath = Path.GetDirectoryName(editorScriptPath).Replace("Editor/", string.Empty) + "/";

            animName = "faceAnim";
            saveFolder = editorFolderPath + "Animations/";
            
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
                    GetAvatarInfo(edittingAvatar.descriptor);
                    ApplySettingsToEditorGUI();
                    avatarMonitorGUI.SetAvatarCam(edittingAvatar.descriptor.gameObject);
                }
            }

            newSDKUI = IsNewSDKUI();

            SceneView.onSceneGUIDelegate += OnSceneGUI;
        }

        private void OnDisable()
        {
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

                                GetAvatarInfo(edittingAvatar.descriptor);

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
                                needRepaint = avatarMonitorGUI.DrawGUI();
                                if (needRepaint) Repaint();

                                if (!needRepaint)
                                    AnimationsGUI(GUILayout.MinWidth(300), GUILayout.MaxHeight(275));
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
                                AvatarInfoGUI();
                            }
                            else if (currentTool == ToolFunc.表情設定)
                            {
                                // 表情設定
                                FaceEmotionGUI();
                            }
                            else if (currentTool == ToolFunc.ProbeAnchor)
                            {
                                // Probe Anchor設定
                                ProbeAnchorGUI();
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
                                needRepaint = avatarMonitorGUI.DrawGUI();
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
                                                AnimationsGUI(GUILayout.Height(200f));
                                        }

                                        // アバター情報
                                        AvatarInfoGUI();

                                    }
                                    else if (currentTool == ToolFunc.表情設定)
                                    {
                                        // 表情設定
                                        FaceEmotionGUI();
                                    }
                                    else if (currentTool == ToolFunc.ProbeAnchor)
                                    {
                                        // Probe Anchor設定
                                        ProbeAnchorGUI();
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

        private void AnimationsGUI(params GUILayoutOption[] option)
        {
            // 設定済みアニメーション一覧
            using (new EditorGUILayout.VerticalScope(GUI.skin.box, option))
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
                    controller = edittingAvatar.standingAnimController;
                }
                else
                {
                    kind = "Sitting";
                    controller = edittingAvatar.sittingAnimController;
                }

                titleText = kind + " Animations";

                EditorGUILayout.LabelField(titleText, EditorStyles.boldLabel);

                if (controller != null)
                {
                    AnimationClip anim;
                    foreach (var handAnim in HANDANIMS)
                    {
                        if (handAnim == controller[handAnim].name)
                            anim = null;
                        else
                            anim = controller[handAnim];

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(handAnim, GUILayout.Width(90));

                            controller[handAnim] = EditorGUILayout.ObjectField(
                                string.Empty,
                                anim,
                                typeof(AnimationClip),
                                true,
                                GUILayout.Width(170)
                            ) as AnimationClip;

                            if (GUILayout.Button("↓", GUILayout.Width(20)))
                            {
                                FaceEmotion.ApplyAnimationProperties(controller[handAnim], ref skinnedMeshList);
                            }
                        }
                    }
                }
                else
                {
                    if (edittingAvatar.descriptor == null)
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
                                var fileName = "CO_" + edittingAvatar.animator.gameObject.name + ".overrideController";
                                edittingAvatar.descriptor.CustomStandingAnims = InstantiateVrcCustomOverideController(saveFolder + "/" + fileName);
                                GetAvatarInfo(edittingAvatar.descriptor);
                            }
                        }
                    }
                }
            }
        }

        private void AvatarInfoGUI()
        {
            #region アバター情報
            if (edittingAvatar.descriptor != null)
            {
                // 性別
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    edittingAvatar.sex = (VRC_AvatarDescriptor.AnimationSet)EditorGUILayout.EnumPopup("Gender", edittingAvatar.sex);

                    if (check.changed) edittingAvatar.descriptor.Animations = edittingAvatar.sex;
                }

                // アップロード状態
                EditorGUILayout.LabelField("Status", (string.IsNullOrEmpty(edittingAvatar.avatarId)) ? "New Avatar" : "Uploaded Avatar");
                edittingAvatar.animator.runtimeAnimatorController = EditorGUILayout.ObjectField(
                    "Animator",
                    edittingAvatar.animator.runtimeAnimatorController,
                    typeof(AnimatorOverrideController),
                    true
                ) as RuntimeAnimatorController;

                // AnimatorOverrideController
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    edittingAvatar.standingAnimController = EditorGUILayout.ObjectField(
                        "Standing Animations",
                        edittingAvatar.standingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;
                    edittingAvatar.sittingAnimController = EditorGUILayout.ObjectField(
                        "Sitting Animations",
                        edittingAvatar.sittingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;

                    if (check.changed)
                    {
                        edittingAvatar.descriptor.CustomStandingAnims = edittingAvatar.standingAnimController;
                        edittingAvatar.descriptor.CustomSittingAnims = edittingAvatar.sittingAnimController;
                    }
                }

                EditorGUILayout.LabelField("Triangles", edittingAvatar.triangleCount + "(" + (edittingAvatar.triangleCount + edittingAvatar.triangleCountInactive) + ")");

                // リップシンク
                string lipSyncWarningMessage = "リップシンクが正しく設定されていない可能性があります";
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    edittingAvatar.lipSyncStyle = (VRC_AvatarDescriptor.LipSyncStyle)EditorGUILayout.EnumPopup("LipSync", edittingAvatar.lipSyncStyle);

                    if (check.changed) edittingAvatar.descriptor.lipSync = edittingAvatar.lipSyncStyle;

                }
                if (edittingAvatar.lipSyncStyle == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        edittingAvatar.faceMesh = EditorGUILayout.ObjectField(
                            "Face Mesh",
                            edittingAvatar.faceMesh,
                            typeof(SkinnedMeshRenderer),
                            true
                        ) as SkinnedMeshRenderer;

                        if (check.changed)
                            edittingAvatar.descriptor.VisemeSkinnedMesh = edittingAvatar.faceMesh;
                    }
                    if (edittingAvatar.faceMesh != null)
                    {
                        isOpeningLipSync = EditorGUILayout.Foldout(isOpeningLipSync, "ShapeKeys");
                        if (isOpeningLipSync)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            using (var scrollView = new EditorGUILayout.ScrollViewScope(lipSyncScrollPos))
                            {
                                lipSyncScrollPos = scrollView.scrollPosition;

                                for (int visemeIndex = 0; visemeIndex < LIPSYNC_SYPEKEY_NUM; visemeIndex++)
                                {
                                    EditorGUILayout.LabelField("Viseme:" + Enum.GetName(typeof(VRC_AvatarDescriptor.Viseme), visemeIndex), edittingAvatar.descriptor.VisemeBlendShapes[visemeIndex]);
                                }
                            }
                        }
                    }
                }
                if (edittingAvatar.lipSyncStyle != VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape || edittingAvatar.faceMesh == null)
                {
                    EditorGUILayout.HelpBox(lipSyncWarningMessage, MessageType.Warning);
                    if (GUILayout.Button("シェイプキーによるリップシンクを自動設定する"))
                    {
                        SetLipSyncToViseme(ref edittingAvatar);
                    }
                }

                EditorGUILayout.Space();
            }
            #endregion
        }

        private void FaceEmotionGUI()
        {
            if (Event.current.type == EventType.ExecuteCommand &&
                Event.current.commandName == "ApplyAnimationProperties") 
            {
                FaceEmotion.ApplyAnimationProperties(sendData.loadingProperties, ref skinnedMeshList);
            }

            EditorGUILayout.LabelField("表情設定", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUI.DisabledScope(edittingAvatar.descriptor == null))
                using (new EditorGUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Load Animation")) 
                    {
                        
                        sendData = CreateInstance<SendData>();
                        var result = FaceEmotion.LoadAnimationProperties(ref sendData, this);

                        if (result)
                            GetWindow<AnimationLoaderGUI>("Animation Loader", true);                
                    }
                }

                if (skinnedMeshList != null)
                {
                    BlendShapeListGUI();
                }

                animName = EditorGUILayout.TextField("AnimClipFileName", animName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("AnimClipSaveFolder", saveFolder);

                    if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
                    {
                        saveFolder = EditorUtility.OpenFolderPanel("Select saved folder", saveFolder, string.Empty);
                        saveFolder = FileUtil.GetProjectRelativePath(saveFolder);
                        if (saveFolder == "/") saveFolder = "Assets/";
                    }

                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    selectedHandAnim = (HandPose.HandPoseType)EditorGUILayout.EnumPopup("HandPose", selectedHandAnim);
                    if (GUILayout.Button("Create AnimFile"))
                    {
                        var animController = edittingAvatar.standingAnimController;

                        var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(animName, saveFolder, ref skinnedMeshList, ref blendshapeExclusions, edittingAvatar.descriptor.gameObject);
                        if (selectedHandAnim != HandPose.HandPoseType.None)
                        {
                            HandPose.AddHandPoseAnimationKeysFromOriginClip(ref createdAnimClip, selectedHandAnim);
                            animController[HANDANIMS[(int)selectedHandAnim - 1]] = createdAnimClip;
                        }

                        edittingAvatar.standingAnimController = animController;
                    }
                    if (GUILayout.Button("Reset All"))
                    {
                        FaceEmotion.ResetAllBlendShapeValues(ref skinnedMeshList);
                    }
                }

                EditorGUILayout.HelpBox("Reset Allを押すとチェックをいれているすべてのシェイプキーの値が最低値になります", MessageType.Warning);

            }
        }

        private void BlendShapeListGUI()
        {
            // BlendShapeのリスト
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scrollView.scrollPosition;
                foreach (var skinnedMesh in skinnedMeshList)
                {
                    skinnedMesh.isOpenBlendShapes = EditorGUILayout.Foldout(skinnedMesh.isOpenBlendShapes, skinnedMesh.objName);
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
                                EditorGUILayout.LabelField("Toggle All", GUILayout.Height(20));
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

                                        if (GUILayout.Button("Min", GUILayout.MaxWidth(50)))
                                        {
                                            FaceEmotion.SetBlendShapeMinValue(ref skinnedMesh.renderer, blendshape.id);
                                        }
                                        if (GUILayout.Button("Max", GUILayout.MaxWidth(50)))
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

        private void ProbeAnchorGUI()
        {
            EditorGUILayout.LabelField("Probe Anchor", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                // 設定するRendererの選択
                isGettingSkinnedMeshRenderer = EditorGUILayout.Toggle("Set To SkinnedMeshRenderer", isGettingSkinnedMeshRenderer);
                isGettingMeshRenderer = EditorGUILayout.Toggle("Set To MeshRenderer", isGettingMeshRenderer);

                // ライティングの計算の基準とする位置を選択
                targetPos = (ProbeAnchor.TARGETPOS)EditorGUILayout.EnumPopup("TargetPosition", targetPos);

                // Rendererの一覧を表示
                if (edittingAvatar.descriptor != null)
                {
                    isOpeningRendererList = EditorGUILayout.Foldout(isOpeningRendererList, "Renderer List");

                    if (isOpeningRendererList)
                    {
                        using (var scrollView = new EditorGUILayout.ScrollViewScope(leftScrollPos))
                        {
                            leftScrollPos = scrollView.scrollPosition;

                            using (new EditorGUI.IndentLevelScope())
                            {
                                int index = 0;
                                
                                if (isGettingSkinnedMeshRenderer && skinnedMeshRendererList != null && isSettingToSkinnedMesh != null)
                                {
                                    foreach (var skinnedMesh in skinnedMeshRendererList)
                                    {
                                        if (skinnedMesh == null) continue;

                                        using (new GUILayout.HorizontalScope())
                                        {
                                            isSettingToSkinnedMesh[index] = EditorGUILayout.Toggle(skinnedMesh.gameObject.name, isSettingToSkinnedMesh[index]);
                                            if (GUILayout.Button("Select"))
                                                Selection.activeGameObject = skinnedMesh.gameObject;
                                        }

                                        index++;
                                    }
                                }

                                index = 0;
                                
                                if (isGettingMeshRenderer && meshRendererList != null && isSettingToMesh != null)
                                {
                                    foreach (var mesh in meshRendererList)
                                    {
                                        if (mesh == null) continue;

                                        using (new GUILayout.HorizontalScope())
                                        {
                                            isSettingToMesh[index] = EditorGUILayout.Toggle(mesh.gameObject.name, isSettingToMesh[index]);
                                            if (GUILayout.Button("Select"))
                                                Selection.activeGameObject = mesh.gameObject;
                                        }

                                        index++;

                                    }
                                }
                            }
                                
                            EditorGUILayout.HelpBox("チェックがついているメッシュのProbeAnchorが設定されます", MessageType.Info);
                        }
                    }
                }
            }

            if (GUILayout.Button("Set ProbeAnchor"))
            {
                GameObject anchorTarget = null;
                var result = ProbeAnchor.CreateAndSetProbeAnchorObject(edittingAvatar.descriptor.gameObject, targetPos, ref anchorTarget);
                if (result && isGettingSkinnedMeshRenderer)
                    ProbeAnchor.SetProbeAnchorToSkinnedMeshRenderers(ref anchorTarget, ref skinnedMeshRendererList, ref isSettingToSkinnedMesh);
                if (result && isGettingMeshRenderer)
                    ProbeAnchor.SetProbeAnchorToMeshRenderers(ref anchorTarget, ref meshRendererList, ref isSettingToMesh);}
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
            EditorGUILayout.LabelField("FaceEmotion Creator", EditorStyles.boldLabel);

            selectedSortType = (SortType)EditorGUILayout.EnumPopup("SortType", selectedSortType);

            isOpeningBlendShapeExclusionList = EditorGUILayout.Foldout(isOpeningBlendShapeExclusionList, "Blendshape Exclusions");
            if (isOpeningBlendShapeExclusionList)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    for (int i = 0; i < blendshapeExclusions.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            blendshapeExclusions[i] =  EditorGUILayout.TextField(blendshapeExclusions[i]);
                            if (GUILayout.Button("Remove"))
                                blendshapeExclusions.RemoveAt(i);
                        }
                    }
                }

                using (new GUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Add"))
                        blendshapeExclusions.Add(string.Empty);
                }
            }

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
                if (skinnedMeshList != null)
                {
                    for (int i = 0; i < skinnedMeshList.Count; i++)
                    {
                        if (edittingAvatar.lipSyncShapeKeyNames != null && edittingAvatar.lipSyncShapeKeyNames.Count > 0)
                            skinnedMeshList[i].SetExclusionBlendShapesByContains(blendshapeExclusions.Union(edittingAvatar.lipSyncShapeKeyNames).ToList<string>());
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

            selectedSortType = settingAsset.selectedSortType;
            blendshapeExclusions = new List<string>(settingAsset.blendshapeExclusions);

            isActiveOnlySelectedAvatar = settingAsset.isActiveOnlySelectedAvatar;
            layoutType = settingAsset.layoutType;

            avatarMonitorGUI.LoadSettingData(settingAsset);
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

            settingAsset.selectedSortType = selectedSortType;
            settingAsset.blendshapeExclusions = new List<string>(blendshapeExclusions);

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
        /// アバターの情報を取得する
        /// </summary>
        private void GetAvatarInfo(VRC_AvatarDescriptor descriptor)
        {
            if (descriptor == null) return;

            var avatarObj = descriptor.gameObject;

            edittingAvatar.animator = avatarObj.GetComponent<Animator>();

            edittingAvatar.eyePos = descriptor.ViewPosition;
            edittingAvatar.sex = descriptor.Animations;

            edittingAvatar.standingAnimController = descriptor.CustomStandingAnims;
            edittingAvatar.sittingAnimController = descriptor.CustomSittingAnims;
            
            edittingAvatar.avatarId = descriptor.gameObject.GetComponent<PipelineManager>().blueprintId;

            edittingAvatar.faceMesh = descriptor.VisemeSkinnedMesh;

            if (edittingAvatar.faceMesh != null && descriptor.lipSync == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
            {
                edittingAvatar.lipSyncShapeKeyNames = new List<string>();
                edittingAvatar.lipSyncShapeKeyNames.AddRange(descriptor.VisemeBlendShapes);
            }

            edittingAvatar.materials = GetMaterials(avatarObj);

            var triangleCountInactive = edittingAvatar.triangleCountInactive;
            edittingAvatar.triangleCount = GetAllTrianglesCount(avatarObj, ref triangleCountInactive);
            edittingAvatar.triangleCountInactive = triangleCountInactive;

            edittingAvatar.lipSyncStyle = descriptor.lipSync;

            // FaceEmotion
            skinnedMeshList = FaceEmotion.GetSkinnedMeshListOfBlendShape(avatarObj);

            // ProbeAnchor
            skinnedMeshRendererList = GetSkinnedMeshList(avatarObj);
            isSettingToSkinnedMesh = new bool[skinnedMeshRendererList.Count];
            for (int i = 0; i < skinnedMeshRendererList.Count; i++) isSettingToSkinnedMesh[i] = true;
            meshRendererList = GetMeshList(avatarObj);
            isSettingToMesh = new bool[meshRendererList.Count];
            for (int i = 0; i < meshRendererList.Count; i++) isSettingToMesh[i] = true;

        }

        /// <summary>
        /// 設定を反映する
        /// </summary>
        private void ApplySettingsToEditorGUI()
        {
            if (edittingAvatar.descriptor == null) return;

            foreach (var skinnedMesh in skinnedMeshList)
            {
                if (edittingAvatar.lipSyncShapeKeyNames != null && edittingAvatar.lipSyncShapeKeyNames.Count > 0)
                    skinnedMesh.SetExclusionBlendShapesByContains(blendshapeExclusions.Union(edittingAvatar.lipSyncShapeKeyNames).ToList<string>());

                if (selectedSortType == SortType.AToZ)
                    skinnedMesh.SortBlendShapesToAscending();
                else
                    skinnedMesh.ResetDefaultSort();
            }
        }

        /// <summary>
        /// obj以下のすべてのメッシュの数を取得する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private int GetAllTrianglesCount(GameObject obj, ref int countInactive)
        {
            int count = 0;
            countInactive = 0;

            var skinnedMeshList = GetSkinnedMeshList(obj);
            var meshList = GetMeshList(obj);

            if (skinnedMeshList != null)
            {
                foreach (var skinnedMeshRenderer in skinnedMeshList)
                {
                    if (skinnedMeshRenderer.sharedMesh == null) continue;

                    if (skinnedMeshRenderer.gameObject.activeSelf)
                        count += skinnedMeshRenderer.sharedMesh.triangles.Length / 3;
                    else
                        countInactive += skinnedMeshRenderer.sharedMesh.triangles.Length / 3;
                }
            }

            if (meshList != null)
            {
                foreach (var meshRenderer in meshList)
                {
                    var meshFilter = meshRenderer.gameObject.GetComponent<MeshFilter>();
                    if (meshFilter == null) continue;
                    else if (meshFilter.sharedMesh == null) continue;

                    if (meshFilter.gameObject.activeSelf)
                        count += meshFilter.sharedMesh.triangles.Length / 3;
                    else
                        countInactive += meshFilter.sharedMesh.triangles.Length / 3;
                }
            }

            return count;
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
        /// 指定オブジェクト以下のSkinnedMeshRendererのリストを取得する
        /// </summary>
        /// <param name="parentObj">親オブジェクト</param>
        /// <returns>SkinnedMeshRendererのリスト</returns>
        public static List<SkinnedMeshRenderer> GetSkinnedMeshList(GameObject parentObj)
        {
            var skinnedMeshList = new List<SkinnedMeshRenderer>();

            var skinnedMeshes = parentObj.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (var skinnedMesh in skinnedMeshes)
            {
                skinnedMeshList.Add(skinnedMesh);
            }

            return skinnedMeshList;
        }

        /// <summary>
        /// 指定オブジェクト以下のMeshRendererのリストを取得する
        /// </summary>
        /// <param name="parentObj">親オブジェクト</param>
        /// <returns>MeshRendererのリスト</returns>
        private List<MeshRenderer> GetMeshList(GameObject parentObj)
        {
            var meshList = new List<MeshRenderer>();

            var meshes = parentObj.GetComponentsInChildren<MeshRenderer>(true);

            foreach (var mesh in meshes)
            {
                meshList.Add(mesh);
            }

            return meshList;
        }

        /// <summary>
        /// Avatarにシェイプキー基準のLipSyncの設定をおこなう
        /// </summary>
        private bool SetLipSyncToViseme(ref Avatar avatar)
        {
            if (avatar == null) return false;

            var desc = avatar.descriptor;
            if (desc == null) return false;

            avatar.lipSyncStyle = VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
            desc.lipSync = VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;

            if (avatar.faceMesh == null)
            {
                var rootObj = avatar.animator.gameObject;
                avatar.faceMesh = rootObj.GetComponentInChildren<SkinnedMeshRenderer>();
                desc.VisemeSkinnedMesh = avatar.faceMesh;
            }

            if (avatar.faceMesh == null) return false;
            var faseMesh = avatar.faceMesh.sharedMesh;

            for (int visemeIndex = 0; visemeIndex < Enum.GetNames(typeof(VRC_AvatarDescriptor.Viseme)).Length; visemeIndex++)
            {
                // VRC用アバターとしてよくあるシェイプキーの名前を元に自動設定
                var visemeShapeKeyName = "vrc.v_" + Enum.GetName(typeof(VRC_AvatarDescriptor.Viseme), visemeIndex).ToLower();
                if (faseMesh.GetBlendShapeIndex(visemeShapeKeyName) == -1) continue;
                desc.VisemeBlendShapes[visemeIndex] = visemeShapeKeyName;
            }
            
            return true;
        }

        /// <summary>
        /// VRChat用のCustomOverrideControllerの複製を取得する
        /// </summary>
        /// <param name="animFolder"></param>
        /// <returns></returns>
        private AnimatorOverrideController InstantiateVrcCustomOverideController(string newFilePath)
        {
            string path = GetVRCSDKFilePath("CustomOverrideEmpty");

            newFilePath = AssetDatabase.GenerateUniqueAssetPath(newFilePath);
            AssetDatabase.CopyAsset(path, newFilePath);
            var overrideController = AssetDatabase.LoadAssetAtPath(newFilePath, typeof(AnimatorOverrideController)) as AnimatorOverrideController;

            return overrideController;
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
        private string GetVRCSDKFilePath(string fileName)
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

        #endregion

        #region Shader Setter

        /// <summary>
        /// obj以下のメッシュに設定されたマテリアルを全て取得する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private List<Material> GetMaterials(GameObject obj)
        {
            var materials = new List<Material>();

            var skinnedMeshes = GetSkinnedMeshList(obj);
            var meshes = GetMeshList(obj);

            foreach (var skinnedMesh in skinnedMeshes)
            {
                foreach (var mat in skinnedMesh.sharedMaterials)
                {
                    materials.Add(mat);
                }
            }

            foreach (var mesh in meshes)
            {
                foreach (var mat in mesh.sharedMaterials)
                {
                    materials.Add(mat);
                }
            }

            materials = materials.Distinct().ToList<Material>();

            return materials;
        }

        #endregion
    }

}