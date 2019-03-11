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
        private const string TOOL_VERSION = "beta v0.2";
        private const string TWITTER_ID = "gatosyocora";
        private const string DISCORD_ID = "gatosyocora#9575";
        private const string MANUAL_URL = "https://docs.google.com/document/d/1DU7mP5PTvERqHzZiiCBJ9ep5CilQ1iaXC_3IoiuPEgA/edit?usp=sharing";

        private GameObject m_avatarCam = null;
        private RenderTexture m_renderTexture;

        // Avatarの情報
        private Animator m_animator;
        private VRC_AvatarDescriptor m_avatar;
        private Vector3 m_eyePos;
        private AnimatorOverrideController standingAnimController;
        private AnimatorOverrideController sittingAnimController;
        private VRC_AvatarDescriptor.AnimationSet m_sex;
        private string m_avatarId = "";
        private int m_overridesNum = 0;
        private PipelineManager m_pipe;
        private SkinnedMeshRenderer m_faceMesh;
        private List<Material> m_materials;
        private int m_triangleCount;
        private int m_triangleCountInactive;

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
        
        private Light m_light;
        private bool isLightPressing = false;

        private Texture upDownTexture;

        #endregion

        #region Animations Variable

        private Vector2 animOverScrollPos = Vector2.zero;

        private readonly string[] HANDANIMS = { "FIST", "FINGERPOINT", "ROCKNROLL", "HANDOPEN", "THUMBSUP", "VICTORY", "HANDGUN" };

        #endregion

        #region FaceEmotion Variable

        private string m_animName = "faceAnim";
        private string m_saveFolder = EDITOR_FOLDER_PATH + "Animations/";
        private HandPose.HandPoseType m_selectedHandAnim = HandPose.HandPoseType.None;

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
        private readonly string[] TOOL_FUNCS = { "Avatar Monitor", "SunLight Rotator", "FaceEmotion Creator", "HandPose Adder", "ProbeAnchor Setter", "MeshBounds Setter", "Shader Checker", "HumanoidPose Resetter" };
        private string licenseText;
        private string readmeText;
        private bool isShowingLicense = false;
        private bool isShowingReadme = false;

        private Vector2 licenseScrollPos = Vector2.zero;
        private Vector2 readmeScrollPos = Vector2.zero;

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

            m_renderTexture = AssetDatabase.LoadAssetAtPath<RenderTexture>(ORIGIN_FOLDER_PATH + "AvatarRT.renderTexture");

            licenseText = GetFileTexts(LICENSE_FILE_PATH);

            readmeText = GetFileTexts(README_FILE_PATH);

            m_light = GetDirectionalLight();

            if (m_avatar != null)
            {
                m_animator = null;
                m_avatar = null;
                m_sex = VRC_AvatarDescriptor.AnimationSet.None;
                m_avatarId = "";
                m_overridesNum = 0;
                m_triangleCount = 0;
                m_triangleCountInactive = 0;
                m_faceMesh = null;

                standingAnimController = null;
                sittingAnimController = null;

                m_animName = "faceAnim";
                m_saveFolder = EDITOR_FOLDER_PATH + "Animations/";
            }

            LoadSettingData();
        }

        private void OnDisable()
        {
            if (m_avatarCam != null)
                UnityEngine.Object.DestroyImmediate(m_avatarCam);
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
                        ApplySettingsToAvatar();
                }
            }

            if (!isShowingToolInfo && !isShowingSetting)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    // アバター選択
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        m_avatar = EditorGUILayout.ObjectField(
                            "Avatar",
                            m_avatar,
                            typeof(VRC_AvatarDescriptor),
                            true
                        ) as VRC_AvatarDescriptor;

                        if (check.changed)
                        {
                            // アバター変更時の処理
                            if (m_avatar != null)
                            {
                                SetAvatarActive(m_avatar);

                                GetAvatarInfo(m_avatar);

                                ApplySettingsToAvatar();

                                SetAvatarCam(m_avatar.gameObject);
                            }
                        }
                    }

                    // LayoutType: Defalut
                    if (layoutType == LayoutType.Default)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            AvatarMonitorGUI(new Vector2(256f, 256f));

                            var option = new GUILayoutOption[] { GUILayout.MinWidth(300), GUILayout.MaxHeight(275) };
                            AnimationsGUI(option);
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

                                if (check.changed) MoveAvatarCam();
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
                            AvatarMonitorGUI(new Vector2(512f, 512f));

                            using (new EditorGUILayout.VerticalScope())
                            {
                                // 各種機能
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    using (var check = new EditorGUI.ChangeCheckScope())
                                    {
                                        // タブを描画する
                                        currentTool = (ToolFunc)GUILayout.Toolbar((int)currentTool, ToolTab.TabToggles, ToolTab.TabButtonStyle, ToolTab.TabButtonSize);

                                        if (check.changed) MoveAvatarCam();
                                    }
                                }

                                if (currentTool == ToolFunc.アバター情報)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        var option = new GUILayoutOption[] { GUILayout.Height(200f) };
                                        AnimationsGUI(option);
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
                        HumanoidPose.ResetPose(m_avatar.gameObject);
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

        private void AvatarMonitorGUI(Vector2 MonitorSize)
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
                        var delta = GatoGUILayout.MiniMonitor(m_renderTexture, MonitorSize.x, MonitorSize.y, ref eventType, isGammaCorrection);
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
                            if (m_avatarCam != null)
                                m_avatarCam.transform.Rotate(new Vector3(0, 30, 0));
                        }

                        if (GUILayout.Button("Reset"))
                        {
                            if (m_avatarCam != null)
                            {
                                m_avatarCam.transform.localRotation = Quaternion.identity;
                                MoveAvatarCam();
                            }
                        }

                        if (GUILayout.Button(">"))
                        {
                            if (m_avatarCam != null)
                                m_avatarCam.transform.Rotate(new Vector3(0, -30, 0));
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
                    var lightDelta = GatoGUILayout.LightRotater(m_light, 50f, 50f, ref isLightPressing);
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

        private void AnimationsGUI(GUILayoutOption[] option)
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
                    controller = standingAnimController;
                }
                else
                {
                    titleText = "Sitting Animations";
                    controller = sittingAnimController;
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
                else if (m_avatar == null)
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
            if (m_avatar != null)
            {
                // 性別
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    m_sex = (VRC_AvatarDescriptor.AnimationSet)EditorGUILayout.EnumPopup("Gender", m_sex);

                    if (check.changed) m_avatar.Animations = m_sex;
                }

                // アップロード状態
                EditorGUILayout.LabelField("Status", (m_avatarId == "") ? "New Avatar" : "Uploaded Avatar");
                m_animator.runtimeAnimatorController = EditorGUILayout.ObjectField(
                    "Animator",
                    m_animator.runtimeAnimatorController,
                    typeof(AnimatorOverrideController),
                    true
                ) as RuntimeAnimatorController;

                // AnimatorOverrideController
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    standingAnimController = EditorGUILayout.ObjectField(
                        "Standing Animations",
                        standingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;
                    sittingAnimController = EditorGUILayout.ObjectField(
                        "Sitting Animations",
                        sittingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;

                    if (check.changed)
                    {
                        m_avatar.CustomStandingAnims = standingAnimController;
                        m_avatar.CustomSittingAnims = sittingAnimController;
                    }
                }

                EditorGUILayout.LabelField("Triangles", m_triangleCount + "(" + (m_triangleCount + m_triangleCountInactive) + ")");
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
                                        foreach (var blendshape in skinnedMesh.blendshapes)
                                        {
                                            isExclusionKey = false;

                                            // 除外するキーかどうか調べる
                                            foreach (var exclusionWord in blendshapeExclusions)
                                            {
                                                if (exclusionWord == "" || isExclusionKey) continue;
                                                if ((blendshape.Value).Contains(exclusionWord))
                                                    isExclusionKey = true;
                                            }

                                            if (!isExclusionKey)
                                            {
                                                using (new EditorGUILayout.HorizontalScope())
                                                {
                                                    EditorGUILayout.SelectableLabel(blendshape.Value, GUILayout.Height(20));
                                                    using (var check = new EditorGUI.ChangeCheckScope())
                                                    {
                                                        var value = skinnedMesh.renderer.GetBlendShapeWeight(blendshape.Key);
                                                        value = EditorGUILayout.Slider(value, 0, 100);
                                                        if (check.changed)
                                                            skinnedMesh.renderer.SetBlendShapeWeight(blendshape.Key, value);
                                                    }

                                                    if (GUILayout.Button("Min", GUILayout.MaxWidth(50)))
                                                    {
                                                        skinnedMesh.renderer.SetBlendShapeWeight(blendshape.Key, 0);
                                                    }
                                                    if (GUILayout.Button("Max", GUILayout.MaxWidth(50)))
                                                    {
                                                        skinnedMesh.renderer.SetBlendShapeWeight(blendshape.Key, 100);
                                                    }
                                                }
                                            }
                                        }
                                        /*
                                        for (int i = 0; i < skinnedMesh.blendShapeNum; i++)
                                        {
                                            isExclusionKey = false;

                                            // 除外するキーかどうか調べる
                                            foreach (var exclusionWord in blendshapeExclusions)
                                            {
                                                if (exclusionWord == "" || isExclusionKey) continue;
                                                if (skinnedMesh.blendshapes[i].Contains(exclusionWord))
                                                    isExclusionKey = true;
                                            }

                                            if (!isExclusionKey)
                                            {
                                                using (new EditorGUILayout.HorizontalScope())
                                                {
                                                    EditorGUILayout.SelectableLabel(skinnedMesh.blendshapes[i], GUILayout.Height(20));
                                                    //skinnedMesh.renderer.SetBlendShapeWeight(i, EditorGUILayout.Slider(skinnedMesh.renderer.GetBlendShapeWeight(i), 0, 100));
                                                    skinnedMesh.renderer.SetBlendShapeWeight(skinnedMesh.blendShapeIndexs[i], EditorGUILayout.Slider(skinnedMesh.renderer.GetBlendShapeWeight(skineblendShapeIndexs[i]), 0, 100));
                                                    if (GUILayout.Button("Min", GUILayout.MaxWidth(50)))
                                                    {
                                                        skinnedMesh.renderer.SetBlendShapeWeight(i, 0);
                                                    }
                                                    if (GUILayout.Button("Max", GUILayout.MaxWidth(50)))
                                                    {
                                                        skinnedMesh.renderer.SetBlendShapeWeight(i, 100);
                                                    }
                                                }
                                            }
                                        }
                                        */
                                    }
                                }
                            }
                        }
                    }
                }

                m_animName = EditorGUILayout.TextField("AnimClipFileName", m_animName);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("AnimClipSaveFolder", m_saveFolder);

                    if (GUILayout.Button("Select Folder", GUILayout.Width(100)))
                    {
                        m_saveFolder = EditorUtility.OpenFolderPanel("Select saved folder", m_saveFolder, "");
                        var match = Regex.Match(m_saveFolder, @"Assets/.*");
                        Debug.Log(match.Value);
                        m_saveFolder = match.Value + "/";
                        if (m_saveFolder == "/") m_saveFolder = "Assets/";
                    }

                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    m_selectedHandAnim = (HandPose.HandPoseType)EditorGUILayout.EnumPopup("HandPose", m_selectedHandAnim);
                    if (GUILayout.Button("Create AnimFile"))
                    {
                        FaceEmotion.CreateAndSetAnimClip(m_animName, m_saveFolder, skinnedMeshList, ref standingAnimController, m_selectedHandAnim, blendshapeExclusions);
                    }
                    if (GUILayout.Button("Reset All"))
                    {
                        FaceEmotion.ResetBlendShapes(ref skinnedMeshList);
                    }
                }

                EditorGUILayout.HelpBox("Reset Allを押すとすべてのシェイプキーの値が0になります", MessageType.Warning);

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
                if (m_avatar != null)
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
                ProbeAnchor.SetProbeAnchor(m_avatar.gameObject, targetPos, ref skinnedMeshRendererList, ref meshRendererList, isSettingToSkinnedMesh, isSettingToMesh, isGettingSkinnedMeshRenderer, isGettingMeshRenderer);
            }
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
                MeshBounds.BoundsSetter(m_avatar.gameObject, exclusions, boundsScale);
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
                        if (m_materials != null)
                        {
                            foreach (var mat in m_materials)
                            {
                                if (mat == null) continue;
                                if (mat.shader == null) continue;

                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorGUILayout.LabelField(mat.shader.name);
                                    EditorGUILayout.LabelField("("+mat.name+")");
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
            
            isShowingReadme = EditorGUILayout.Foldout(isShowingReadme, "Readme");

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


            isShowingLicense =  EditorGUILayout.Foldout(isShowingLicense, "License");

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
                            if (GUILayout.Button("Clear"))
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
                SaveSettingData();
            }
            if (GUILayout.Button("Default Setting"))
            {
                DeleteMySettingData();
                LoadSettingData();
            }

        }

        #region General Functions

        /// <summary>
        /// 設定情報を読み込む
        /// </summary>
        private void LoadSettingData()
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
        private void SaveSettingData()
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
            if (m_avatarCam != null)
                DestroyImmediate(m_avatarCam);

            var avatarCam = AssetDatabase.LoadAssetAtPath<GameObject>(ORIGIN_FOLDER_PATH + "AvatarCam.prefab");
            m_avatarCam = PrefabUtility.InstantiatePrefab(avatarCam) as GameObject;

            if (m_eyePos != null) maxCamHeight = m_eyePos.y;

            SetAvatarCamBgColor(monitorBgColor);

            MoveAvatarCam();
        }

        /// <summary>
        /// アバターモニターの背景色を設定する
        /// </summary>
        /// <param name="col"></param>
        private void SetAvatarCamBgColor(Color col)
        {
            if (m_avatarCam == null) return;

            var mainTrans = m_avatarCam.transform.GetChild(0);
            var camera = mainTrans.GetComponent<Camera>();
            camera.backgroundColor = col;
        }

        /// <summary>
        /// アバターの情報を取得する
        /// </summary>
        private void GetAvatarInfo(VRC_AvatarDescriptor avatar)
        {
            if (avatar == null) return;
            
            m_animator = avatar.gameObject.GetComponent<Animator>();

            m_eyePos = avatar.ViewPosition;
            m_sex = avatar.Animations;

            standingAnimController = avatar.CustomStandingAnims;
            sittingAnimController = avatar.CustomSittingAnims;

            m_pipe = avatar.gameObject.GetComponent<PipelineManager>();
            m_avatarId = m_pipe.blueprintId;

            m_faceMesh = avatar.VisemeSkinnedMesh;

            m_materials = GetMaterials(avatar.gameObject);

            m_triangleCount = GetAllTrianglesCount(avatar.gameObject, ref m_triangleCountInactive);

            // FaceEmotion
            skinnedMeshList = FaceEmotion.GetSkinnedMeshListOfBlendShape(avatar.gameObject);

            // ProbeAnchor
            skinnedMeshRendererList = GetSkinnedMeshList(avatar.gameObject);
            isSettingToSkinnedMesh = new bool[skinnedMeshRendererList.Count];
            for (int i = 0; i < skinnedMeshRendererList.Count; i++) isSettingToSkinnedMesh[i] = true;
            meshRendererList = GetMeshList(avatar.gameObject);
            isSettingToMesh = new bool[meshRendererList.Count];
            for (int i = 0; i < meshRendererList.Count; i++) isSettingToMesh[i] = true;

        }

        /// <summary>
        /// 設定を反映する
        /// </summary>
        private void ApplySettingsToAvatar()
        {
            if (m_avatar == null) return;

            foreach (var skinnedMesh in skinnedMeshList)
            {
                if (selectedSortType == SortType.AToZ)
                    skinnedMesh.SortBlendShapes();
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
            if (m_avatarCam == null || m_avatar == null) return;

            var nowPos = m_avatarCam.transform.position;
            var avatarPos = m_avatar.transform.position;
            var childTrans = m_avatarCam.transform.Find("Main").gameObject.transform;

            // 顔にあわせる
            if (currentTool == ToolFunc.表情設定)
            {
                cameraHeight = m_eyePos.y;
                m_avatarCam.transform.position = new Vector3(nowPos.x, cameraHeight + avatarPos.y, nowPos.z);
                childTrans.localPosition = new Vector3(0, 0, faceZoomDist);
                camPosZ = faceZoomDist;
            }
            else
            {
                cameraHeight = (maxCamHeight > 1)?1:maxCamHeight;
                m_avatarCam.transform.position = new Vector3(nowPos.x, cameraHeight + avatarPos.y, nowPos.z);
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
            if (m_avatarCam == null || m_avatar == null) return;
            var nowPos = m_avatarCam.transform.position;
            var avatarPos = m_avatar.transform.position;
            m_avatarCam.transform.position = new Vector3(nowPos.x, avatarPos.y + value, nowPos.z);
        }

        /// <summary>
        /// AvatarCamを回転させる
        /// </summary>
        /// <param name="delta"></param>
        private void RotateAvatarCam(Vector2 delta)
        {
            if (m_avatarCam == null || delta == Vector2.zero) return;

            m_avatarCam.transform.Rotate(new Vector3(-delta.y, delta.x, 0));
            Repaint();
        }

        /// <summary>
        /// AvatarCamをズームさせる(マウスホイール)
        /// </summary>
        /// <param name="delta"></param>
        private void ZoomAvatarCam(Vector2 delta, float level)
        {
            if (m_avatarCam == null || delta == Vector2.zero) return;

            var cam = m_avatarCam.transform.GetChild(0);
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
            if (m_avatarCam == null) return;
            var cam = m_avatarCam.transform.GetChild(0);
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
            if (m_light == null) return;

            (m_light.gameObject).transform.Rotate(new Vector3(0, 1, 0), -delta.x);
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

            return materials;
        }

        #endregion
    }

}