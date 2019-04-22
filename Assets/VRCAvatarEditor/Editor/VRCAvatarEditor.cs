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
        
        private GameObject avatarCam = null;
        private RenderTexture avatarCamTexture;
        private const int CAMERA_ROTATE_ANGLE = 30;

        // Avatarの情報
        private class Avatar
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

            public Avatar()
            {
                animator = null;
                descriptor = null;
                eyePos = Vector3.zero;
                standingAnimController = null;
                sittingAnimController = null;
                sex = VRC_AvatarDescriptor.AnimationSet.None;
                avatarId = "";
                overridesNum = 0;
                faceMesh = null;
                lipSyncShapeKeyNames = null;
                triangleCount = 0;
                triangleCountInactive = 0;
            }
            
        };
        private Avatar edittingAvatar = null;

        private const string EDITOR_FOLDER_PATH = "Assets/VRCAvatarEditor/";
        private const string ORIGIN_FOLDER_PATH = EDITOR_FOLDER_PATH + "Origins/";

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

        private enum ToolFunc
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

        private static class GatoGUILayout
        {
            #region VericalSlider Variable
            private static float sliderPos;
            #endregion

            #region MiniMonitor Variable
            private static Material gammaMat = AssetDatabase.LoadAssetAtPath<Material>(GAMMA_MATRIAL_PATH);
            #endregion

            #region LightRotater Variable
            private static Texture lightActiveTex = AssetDatabase.LoadAssetAtPath<Texture>(ORIGIN_FOLDER_PATH + "Sun_ON.png");
            private static Texture lightInactiveTex = AssetDatabase.LoadAssetAtPath<Texture>(ORIGIN_FOLDER_PATH + "Sun_OFF.png");
            #endregion

            public static float VerticalSlider(Texture texture, float texSize, float height, float value, float minValue, float maxValue)
            {
                sliderPos = value/(maxValue+minValue);

                var rect = GUILayoutUtility.GetRect(10f, height);

                var boxRect = new Rect(rect.position, new Vector2(7f, height));
                GUI.Box(boxRect, "");

                var texRect = new Rect( 
                                    rect.position.x - texSize / 2f + 3f,
                                    rect.position.y + (rect.height - sliderPos * height) - texSize / 2f,
                                    texSize, texSize
                              );

                var e = Event.current;

                if (texRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDrag)
                    {
                        GUI.changed = true;

                        var diff = e.delta.y / height;

                        if (sliderPos - diff <= 1 && sliderPos - diff >= 0)
                            sliderPos -= diff;
                        else if (sliderPos - diff > 1)
                            sliderPos = 1;
                        else if (sliderPos - diff < 0)
                            sliderPos = 0;
                    }
                }

                GUI.DrawTexture(texRect, texture);

                value = sliderPos * (maxValue + minValue);

                return value;
            }

            public static float HorizontalSlider(Texture texture, float texSize, float width, float value, float minValue, float maxValue)
            {
                sliderPos = value / (maxValue + minValue);

                var rect = GUILayoutUtility.GetRect(width, 10f);

                Debug.Log(rect.position);
                var boxRect = new Rect(rect.position, new Vector2(width, 7f));
                GUI.Box(boxRect, "");

                var texRect = new Rect(
                                    rect.position.x + (rect.width - sliderPos * width) - texSize / 2f ,
                                    rect.position.y - texSize / 2f + 3f,
                                    texSize, texSize
                              );

                var e = Event.current;

                if (texRect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDrag)
                    {
                        GUI.changed = true;

                        var diff = e.delta.x / width;

                        if (sliderPos - diff <= 1 && sliderPos - diff >= 0)
                            sliderPos -= diff;
                        else if (sliderPos - diff > 1)
                            sliderPos = 1;
                        else if (sliderPos - diff < 0)
                            sliderPos = 0;
                    }
                }

                GUI.DrawTexture(texRect, texture);

                value = sliderPos * (maxValue + minValue);

                return value;
            }

            public static Vector2 MiniMonitor(Texture texture, float width, float height, ref int type, bool isGammaCorrection)
            {
                var rect = GUILayoutUtility.GetRect(width, height, GUI.skin.box);

                //GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, false, 0);
                Graphics.DrawTexture(rect, texture, (isGammaCorrection) ? gammaMat : null);

                var e = Event.current;

                if (rect.Contains(e.mousePosition))
                {
                    if (e.type == EventType.MouseDrag)
                    {
                        type = (int)e.type;
                        return e.delta;
                    }
                    else if (e.type == EventType.ScrollWheel)
                    {
                        type = (int)e.type;
                        return e.delta;
                    }
                }

                return Vector2.zero;
            }

            public static Vector2 LightRotater(Light light, float width, float height, ref bool isPressing)
            {
                var isExistLight = (light != null && light.gameObject.activeInHierarchy);

                var rect = GUILayoutUtility.GetRect(width, height, GUI.skin.box);
                var texture = (isExistLight) ? lightActiveTex : lightInactiveTex;

                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true, 0);

                var e = Event.current;

                if (isExistLight)
                {

                    if (rect.Contains(e.mousePosition) && e.type == EventType.MouseDown)
                    {
                        isPressing = true;
                    }
                    else if (isPressing && e.type == EventType.MouseUp)
                    {
                        isPressing = false;
                    }

                    if (e.type == EventType.MouseDrag && isPressing)
                    {
                        return e.delta;
                    }

                }

                return Vector2.zero;
            }
        }

        #region AvatarMonitor Variable

        private const string GAMMA_MATRIAL_PATH = EDITOR_FOLDER_PATH + "Origins/Gamma.mat";

        private float cameraHeight = 1;
        private float maxCamHeight = 1;
        private float minCamHeight = 0;
        private float camPosZ;

        private float zoomLevel = 1.0f;
        
        private Light sceneLight;
        private bool isLightPressing = false;

        private Texture upDownTexture;

        #endregion

        #region Animations Variable

        private Vector2 animOverScrollPos = Vector2.zero;

        private readonly string[] HANDANIMS = { "FIST", "FINGERPOINT", "ROCKNROLL", "HANDOPEN", "THUMBSUP", "VICTORY", "HANDGUN" };

        #endregion

        #region FaceEmotion Variable

        private string animName = "faceAnim";
        private string saveFolder = EDITOR_FOLDER_PATH + "Animations/";
        private HandPose.HandPoseType selectedHandAnim = HandPose.HandPoseType.None;

        private List<SkinnedMesh> skinnedMeshList = null;
        private Vector2 scrollPos = Vector2.zero;

        private bool isExclusionKey;

        public enum SortType
        {
            UnSort,
            AToZ,
        }

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
        private Vector3 boundsScale = new Vector3(1, 2, 1);
        private List<GameObject> exclusions = new List<GameObject>();
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
        private const string LICENSE_FILE_PATH = EDITOR_FOLDER_PATH + "LICENSE.txt";
        private const string README_FILE_PATH = EDITOR_FOLDER_PATH + "README.txt";
        private const string USING_SOFTWARE_LICENSE_FILE_PATH = EDITOR_FOLDER_PATH + "USING_SOFTWARE_LICENSES.txt";
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

        private float defaultZoomDist = 1.0f;
        private float faceZoomDist = 0.5f;
        private float zoomStepDist = 0.25f;

        private bool isGammaCorrection = true;
        private Color monitorBgColor = new Color(0.95f, 0.95f, 0.95f, 1);

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
            upDownTexture = AssetDatabase.LoadAssetAtPath<Texture>(ORIGIN_FOLDER_PATH + "UpDown.png");

            avatarCamTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(ORIGIN_FOLDER_PATH + "AvatarRT.renderTexture");

            licenseText = GetFileTexts(LICENSE_FILE_PATH);

            readmeText = GetFileTexts(README_FILE_PATH);

            usingSoftwareLicenseText = GetFileTexts(USING_SOFTWARE_LICENSE_FILE_PATH);

            sceneLight = GetDirectionalLight();

            edittingAvatar = new Avatar();
                
            animName = "faceAnim";
            saveFolder = EDITOR_FOLDER_PATH + "Animations/";

            LoadSettingDataFromScriptableObject();
        }

        private void OnDisable()
        {
            if (avatarCam != null)
                UnityEngine.Object.DestroyImmediate(avatarCam);
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
                                SetAvatarActive(edittingAvatar.descriptor);

                                GetAvatarInfo(edittingAvatar.descriptor);

                                ApplySettingsToEditorGUI();

                                SetAvatarCam(edittingAvatar.descriptor.gameObject);
                            }
                        }
                    }

                    // LayoutType: Defalut
                    if (layoutType == LayoutType.Default)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            AvatarMonitorGUI(256f, 256f);
                            
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
                            AvatarMonitorGUI(512f, 512f);

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

                    // ポーズ修正
                    if (GUILayout.Button("Reset Pose"))
                    {
                        HumanoidPose.ResetPose(edittingAvatar.descriptor.gameObject);
                    }

                    // アップロード
                    if (GUILayout.Button("Upload Avatar"))
                    {
                        EditorApplication.ExecuteMenuItem("VRChat SDK/Show Build Control Panel");
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

            //ShaderUI.Shader(100f, 30f);
        }

        private void AvatarMonitorGUI(float monitorSizeX, float monitorSizeY)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    // アバター表示
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        int eventType = 0;
                        var delta = GatoGUILayout.MiniMonitor(avatarCamTexture, monitorSizeX, monitorSizeY, ref eventType, isGammaCorrection);
                        if (!isLightPressing)
                        {
                            if (eventType == (int)EventType.MouseDrag) RotateAvatarCam(delta);
                            else if (eventType == (int)EventType.ScrollWheel) ZoomAvatarCam(delta, zoomLevel);
                        }

                        GUILayout.FlexibleSpace();
                    }

                    // アバター回転
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("<"))
                        {
                            if (avatarCam != null)
                                avatarCam.transform.Rotate(0, CAMERA_ROTATE_ANGLE, 0);
                        }

                        if (GUILayout.Button("Reset"))
                        {
                            if (avatarCam != null)
                            {
                                avatarCam.transform.localRotation = Quaternion.identity;
                                MoveAvatarCam();
                            }
                        }

                        if (GUILayout.Button(">"))
                        {
                            if (avatarCam != null)
                                avatarCam.transform.Rotate(0, -CAMERA_ROTATE_ANGLE, 0);
                        }
                    }

                    using (new GUILayout.HorizontalScope())
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        //GUILayout.FlexibleSpace();
                        zoomLevel = EditorGUILayout.Slider(zoomLevel, 0f, 1f);
                        //zoomLevel = GatoGUILayout.HorizontalSlider(upDownTexture, 20f, 200f, zoomLevel, 0f, 1f);
                        //GUILayout.FlexibleSpace();

                        if (check.changed) ZoomAvatarCam(zoomLevel);
                    }
                }


            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    var lightDelta = GatoGUILayout.LightRotater(sceneLight, 50f, 50f, ref isLightPressing);
                    RotateLight(lightDelta);
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(20f);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        cameraHeight = GatoGUILayout.VerticalSlider(upDownTexture, 30f, 150f, cameraHeight, minCamHeight, maxCamHeight);
                        if (check.changed) MoveAvatarCamHeight(cameraHeight);
                    }
                    GUILayout.FlexibleSpace();
                }
            }
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

                string titleText;
                AnimatorOverrideController controller;
                if (_tab == Tab.Standing)
                {
                    titleText = "Standing Animations";
                    controller = edittingAvatar.standingAnimController;
                }
                else
                {
                    titleText = "Sitting Animations";
                    controller = edittingAvatar.sittingAnimController;
                }

                EditorGUILayout.LabelField(titleText, EditorStyles.boldLabel);

                if (controller != null)
                {
                    using (var scrollView = new EditorGUILayout.ScrollViewScope(animOverScrollPos))
                    {
                        animOverScrollPos = scrollView.scrollPosition;
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
                                    "",
                                    anim,
                                    typeof(AnimationClip),
                                    true,
                                    GUILayout.Width(170)
                                ) as AnimationClip;

                                if (GUILayout.Button("R", GUILayout.Width(20)))
                                {
                                    controller[handAnim] = null;
                                }
                            }
                        }
                    }
                }
                else if (edittingAvatar.descriptor == null)
                {
                    EditorGUILayout.HelpBox("Not Setting Avatar", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox("Not Setting Custom Standing Anims", MessageType.Warning);
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
                EditorGUILayout.LabelField("Status", (edittingAvatar.avatarId == "") ? "New Avatar" : "Uploaded Avatar");
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
            }
            #endregion
        }

        private void FaceEmotionGUI()
        {
            EditorGUILayout.LabelField("表情設定", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                using (new EditorGUI.IndentLevelScope())
                {

                    if (skinnedMeshList != null)
                    {
                        BlendShapeListGUI();
                    }
                }

                animName = EditorGUILayout.TextField("AnimClipFileName", animName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("AnimClipSaveFolder", saveFolder);

                    if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
                    {
                        saveFolder = EditorUtility.OpenFolderPanel("Select saved folder", saveFolder, "");
                        var match = Regex.Match(saveFolder, @"Assets/.*");
                        Debug.Log(match.Value);
                        saveFolder = match.Value + "/";
                        if (saveFolder == "/") saveFolder = "Assets/";
                    }

                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    selectedHandAnim = (HandPose.HandPoseType)EditorGUILayout.EnumPopup("HandPose", selectedHandAnim);
                    if (GUILayout.Button("Create AnimFile"))
                    {
                        var animController = edittingAvatar.standingAnimController;

                        var createdAnimClip = FaceEmotion.CreateBlendShapeAnimationClip(animName, saveFolder, ref skinnedMeshList, ref blendshapeExclusions);
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
                                    skinnedMesh.isContainsAll = EditorGUILayout.ToggleLeft("", skinnedMesh.isContainsAll, GUILayout.Width(45));
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
                                        blendshape.isContains = EditorGUILayout.ToggleLeft("", blendshape.isContains, GUILayout.Width(45));

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
                GameObject anchorTarget = new GameObject();
                ProbeAnchor.CreateAndSetProbeAnchorObject(edittingAvatar.descriptor.gameObject, targetPos, ref anchorTarget);
                if (isGettingSkinnedMeshRenderer)
                    ProbeAnchor.SetProbeAnchorToSkinnedMeshRenderers(ref anchorTarget, ref skinnedMeshRendererList, ref isSettingToSkinnedMesh);
                if (isGettingMeshRenderer)
                    ProbeAnchor.SetProbeAnchorToMeshRenderers(ref anchorTarget, ref meshRendererList, ref isSettingToMesh);}
        }

        private void MeshBoundsGUI()
        {
            EditorGUILayout.LabelField("Bounds", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                boundsScale = EditorGUILayout.Vector3Field("Bounds Scale", boundsScale);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Exclusions");

                    if (GUILayout.Button("+"))
                    {
                        exclusions.Add(null);
                    }
                    if (GUILayout.Button("-"))
                    {
                        if (exclusions.Count > 0)
                            exclusions.RemoveAt(exclusions.Count - 1);
                    }
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    for (int i = 0; i < exclusions.Count; i++)
                    {
                        exclusions[i] = EditorGUILayout.ObjectField(
                            "Object " + (i + 1),
                            exclusions[i],
                            typeof(GameObject),
                            true
                        ) as GameObject;
                    }
                }
            }

            if (GUILayout.Button("Set Bounds"))
            {
                MeshBounds.BoundsSetter(edittingAvatar.descriptor.gameObject, exclusions, boundsScale);
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

            EditorGUILayout.LabelField("AvatarMonitor", EditorStyles.boldLabel);
            defaultZoomDist = EditorGUILayout.FloatField("Default Camera Distance", defaultZoomDist);
            faceZoomDist = EditorGUILayout.FloatField("Face Camera Distance", faceZoomDist);
            zoomStepDist = EditorGUILayout.FloatField("Camera Zoom Step Distance", zoomStepDist);

            EditorGUILayout.Space();
            isGammaCorrection = EditorGUILayout.ToggleLeft("ガンマ補正", isGammaCorrection);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                monitorBgColor = EditorGUILayout.ColorField("モニター背景色", monitorBgColor);
                if (check.changed) SetAvatarCamBgColor(monitorBgColor);
            }
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
                        blendshapeExclusions.Add("");
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
            MoveAvatarCam();

            if (currentTool == ToolFunc.表情設定)
            {
                if (skinnedMeshList != null)
                {
                    for (int i = 0; i < skinnedMeshList.Count; i++)
                    {
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
            var settingAsset = AssetDatabase.LoadAssetAtPath<SettingData>(ORIGIN_FOLDER_PATH + "CustomSettingData.asset");

            if (settingAsset == null)
                settingAsset = AssetDatabase.LoadAssetAtPath<SettingData>(ORIGIN_FOLDER_PATH + "DefaultSettingData.asset");

            defaultZoomDist = settingAsset.defaultZoomDist;
            faceZoomDist = settingAsset.faceZoomDist;
            zoomStepDist = settingAsset.zoomStepDist;

            isGammaCorrection = settingAsset.isGammaCorrection;
            monitorBgColor = settingAsset.monitorBgColor;

            selectedSortType = settingAsset.selectedSortType;
            blendshapeExclusions = new List<string>(settingAsset.blendshapeExclusions);

            isActiveOnlySelectedAvatar = settingAsset.isActiveOnlySelectedAvatar;
            layoutType = settingAsset.layoutType;
        }

        /// <summary>
        /// 設定情報を保存する
        /// </summary>
        private void SaveSettingDataToScriptableObject()
        {
            bool newCreated = false;
            var settingAsset = AssetDatabase.LoadAssetAtPath<SettingData>(ORIGIN_FOLDER_PATH + "CustomSettingData.asset");

            if (settingAsset == null)
            {
                settingAsset = CreateInstance<SettingData>();
                newCreated = true;
            }

            settingAsset.defaultZoomDist = defaultZoomDist;
            settingAsset.faceZoomDist = faceZoomDist;
            settingAsset.zoomStepDist = zoomStepDist;

            settingAsset.isGammaCorrection = isGammaCorrection;
            settingAsset.monitorBgColor = monitorBgColor;

            settingAsset.selectedSortType = selectedSortType;
            settingAsset.blendshapeExclusions = new List<string>(blendshapeExclusions);

            settingAsset.isActiveOnlySelectedAvatar = isActiveOnlySelectedAvatar;
            settingAsset.layoutType = layoutType;

            if (newCreated)
            {
                AssetDatabase.CreateAsset(settingAsset, ORIGIN_FOLDER_PATH + "CustomSettingData.asset");
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
            var settingAsset = AssetDatabase.LoadAssetAtPath<SettingData>(ORIGIN_FOLDER_PATH + "CustomSettingData.asset");
            if (settingAsset == null) return;

            AssetDatabase.MoveAssetToTrash(ORIGIN_FOLDER_PATH + "CustomSettingData.asset");
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// アバターを写す用のカメラを設定する
        /// </summary>
        private void SetAvatarCam(GameObject obj)
        {
            if (avatarCam != null)
                DestroyImmediate(avatarCam);

            var avatarCam_prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ORIGIN_FOLDER_PATH + "AvatarCam.prefab");
            avatarCam = PrefabUtility.InstantiatePrefab(avatarCam_prefab) as GameObject;

            if (edittingAvatar.eyePos != null) maxCamHeight = edittingAvatar.eyePos.y;

            SetAvatarCamBgColor(monitorBgColor);

            MoveAvatarCam();
        }

        /// <summary>
        /// アバターモニターの背景色を設定する
        /// </summary>
        /// <param name="col"></param>
        private void SetAvatarCamBgColor(Color col)
        {
            if (avatarCam == null) return;

            var mainTrans = avatarCam.transform.GetChild(0);
            var camera = mainTrans.GetComponent<Camera>();
            camera.backgroundColor = col;
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
        /// AvatarCamの位置を動かす
        /// </summary>
        private void MoveAvatarCam()
        {
            if (avatarCam == null || edittingAvatar.descriptor == null) return;

            var nowPos = avatarCam.transform.position;
            var avatarPos = edittingAvatar.descriptor.transform.position;
            var childTrans = avatarCam.transform.Find("Main").gameObject.transform;

            // 顔にあわせる
            if (currentTool == ToolFunc.表情設定)
            {
                cameraHeight = edittingAvatar.eyePos.y;
                avatarCam.transform.position = new Vector3(nowPos.x, cameraHeight + avatarPos.y, nowPos.z);
                childTrans.localPosition = new Vector3(0, 0, faceZoomDist);
                camPosZ = faceZoomDist;
            }
            else
            {
                cameraHeight = (maxCamHeight > 1)?1:maxCamHeight;
                avatarCam.transform.position = new Vector3(nowPos.x, cameraHeight + avatarPos.y, nowPos.z);
                childTrans.localPosition = new Vector3(0, 0, defaultZoomDist);
                camPosZ = defaultZoomDist;
            }

            zoomLevel = 1;
        }

        /// <summary>
        /// AvatarCamの高さを変える
        /// </summary>
        /// <param name="value"></param>
        private void MoveAvatarCamHeight(float value)
        {
            if (avatarCam == null || edittingAvatar.descriptor == null) return;
            var nowPos = avatarCam.transform.position;
            var avatarPos = edittingAvatar.descriptor.transform.position;
            avatarCam.transform.position = new Vector3(nowPos.x, avatarPos.y + value, nowPos.z);
        }

        /// <summary>
        /// AvatarCamを回転させる
        /// </summary>
        /// <param name="delta"></param>
        private void RotateAvatarCam(Vector2 delta)
        {
            if (avatarCam == null || delta == Vector2.zero) return;

            avatarCam.transform.Rotate(new Vector3(-delta.y, delta.x, 0));
            Repaint();
        }

        /// <summary>
        /// AvatarCamをズームさせる(マウスホイール)
        /// </summary>
        /// <param name="delta"></param>
        private void ZoomAvatarCam(Vector2 delta, float level)
        {
            if (avatarCam == null || delta == Vector2.zero) return;

            var cam = avatarCam.transform.GetChild(0);
            cam.transform.Translate(new Vector3(0, 0, (float)(-(delta.y/Mathf.Abs(delta.y)) * zoomStepDist)));
            camPosZ = cam.transform.localPosition.z + zoomStepDist * (1 - level);
            Repaint();
        }

        /// <summary>
        /// AvatarCamをズームさせる（スライダー）
        /// </summary>
        /// <param name="delta"></param>
        private void ZoomAvatarCam(float level)
        {
            if (avatarCam == null) return;
            var cam = avatarCam.transform.GetChild(0);
            cam.transform.localPosition = new Vector3(0, 0, camPosZ - zoomStepDist * (1-level));
            Repaint();
        }

        /// <summary>
        /// DirectionalLightを取得する
        /// </summary>
        /// <returns></returns>
        private Light GetDirectionalLight()
        {

            var lights = Resources.FindObjectsOfTypeAll(typeof(Light)) as Light[];
            
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    if (light.name == "SceneLight") continue;
                    return light;
                }
            }

            return null;
        }

        /// <summary>
        /// DirectionalLightを回転させる
        /// </summary>
        /// <param name="delta"></param>
        private void RotateLight(Vector2 delta)
        {
            if (sceneLight == null) return;

            (sceneLight.gameObject).transform.Rotate(new Vector3(0, 1, 0), -delta.x);
            //(m_light.gameObject).transform.Rotate(new Vector3(1, 0, 0), -delta.y);
            Repaint();
        }

        /// <summary>
        /// pathのファイル内容を取得する
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetFileTexts(string path)
        {
            string text = "";
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