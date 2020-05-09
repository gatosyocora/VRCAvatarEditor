using UnityEngine;
using UnityEditor;
using System;
using VRCSDK2;

namespace VRCAvatarEditor
{
    public class AvatarMonitorGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar avatar;
        private VRCAvatarEditorGUI.ToolFunc currentTool;
            
        private RenderTexture avatarCamTexture;
        private bool isGammaCorrection = true;
        private GameObject avatarCam = null;

        private static readonly int CAMERA_ROTATE_ANGLE = 30;
        
        private float camPosZ;

        private float zoomLevel = 1.0f;

        private Light sceneLight;
        private bool isLightPressing = false;

        private Texture upDownTexture;
        private Material gammaMat;

        public enum MonitorSize
        {
            Small = 256,
            Mediam = 512,
            Lerge = 768,
            Custom
        }

        private float defaultZoomDist = 1.0f;
        private float faceZoomDist = 0.5f;
        private float zoomStepDist = 0.25f;

        private Color monitorBgColor = new Color(0.95f, 0.95f, 0.95f, 1);

        private MonitorSize sizeType = MonitorSize.Small;
        private int monitorSize;

        public AvatarMonitorField avatarMonitorField;

        public void Initialize(VRCAvatarEditorGUI.ToolFunc currentTool)
        {
            this.currentTool = currentTool;

            upDownTexture = Resources.Load<Texture>("Icon/UpDown");

            avatarMonitorField = new AvatarMonitorField();

            MoveAvatarCam += avatarMonitorField.MoveAvatarCam;
        }

        public Action<bool> MoveAvatarCam;

        public void Dispose()
        {
            if (avatarCam != null)
                UnityEngine.Object.DestroyImmediate(avatarCam);

            avatarMonitorField.Dispose();
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    // アバター表示
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();

                        if (avatarMonitorField.Render(monitorSize))
                        {
                            return true;
                        }

                        GUILayout.FlexibleSpace();
                    }

                    // アバター回転
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("<"))
                        {
                            avatarMonitorField.RotateCamera(-CAMERA_ROTATE_ANGLE);
                            return true;
                        }

                        if (GUILayout.Button("Reset"))
                        {
                            avatarMonitorField.ResetCameraTransform();
                            return true;
                        }

                        if (GUILayout.Button(">"))
                        {
                            avatarMonitorField.RotateCamera(CAMERA_ROTATE_ANGLE);
                            return true;
                        }
                    }

                    using (new GUILayout.HorizontalScope())
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        zoomLevel = EditorGUILayout.Slider(zoomLevel, 0f, 1f);

                        if (check.changed)
                        {
                            avatarMonitorField.ZoomAvatarCam(zoomLevel);
                            return true;
                        }
                    }
                }
            }

            using (new GUILayout.VerticalScope())
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    var lightDelta = GatoGUILayout.LightRotater(50f, 50f, ref isLightPressing);

                    if (lightDelta != Vector2.zero)
                    {
                        avatarMonitorField.RotateLight(lightDelta);
                        return true;
                    }
                    GUILayout.FlexibleSpace();
                }

                GUILayout.Space(20f);

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        var normalizedCameraHeight = GatoGUILayout.VerticalSlider(upDownTexture, 30f, 150f, avatarMonitorField.GetNormalizedMonitorHeight(), 0, 1);
                        if (check.changed)
                        {
                            avatarMonitorField.MoveAvatarCamHeight(normalizedCameraHeight);
                            return true;
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            return false;
        }

        public void DrawSettingsGUI()
        {
            EditorGUILayout.LabelField("AvatarMonitor", EditorStyles.boldLabel);
            defaultZoomDist = EditorGUILayout.FloatField("Default Camera Distance", defaultZoomDist);
            faceZoomDist = EditorGUILayout.FloatField("Face Camera Distance", faceZoomDist);
            zoomStepDist = EditorGUILayout.FloatField("Camera Zoom Step Distance", zoomStepDist);

            EditorGUILayout.Space();
            isGammaCorrection = EditorGUILayout.ToggleLeft("ガンマ補正", isGammaCorrection);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                monitorBgColor = EditorGUILayout.ColorField("モニター背景色", monitorBgColor);
                if (check.changed) avatarMonitorField.SetAvatarCamBgColor(monitorBgColor);
            }
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                sizeType = (MonitorSize)EditorGUILayout.EnumPopup("Monitor Size", sizeType);
                if (check.changed && sizeType != MonitorSize.Custom)
                {
                    monitorSize = (int)sizeType;
                }
            }
            using (new EditorGUI.DisabledGroupScope(sizeType != MonitorSize.Custom))
            {
                monitorSize = EditorGUILayout.IntField("Size", monitorSize);
            }
        }


        /// <summary>
        /// アバターを写す用のカメラを設定する
        /// </summary>
        public VRCAvatarEditor.Avatar SetAvatarPreview(VRC_AvatarDescriptor descriptor)
        {
            var avatar = avatarMonitorField.AddAvatar(descriptor);
            avatarMonitorField.SetAvatarCamBgColor(monitorBgColor);
            this.avatar = avatar;

            return avatar;
        }

        public void LoadSettingData(SettingData settingAsset)
        {
            defaultZoomDist = settingAsset.defaultZoomDist;
            faceZoomDist = settingAsset.faceZoomDist;
            zoomStepDist = settingAsset.zoomStepDist;

            isGammaCorrection = settingAsset.isGammaCorrection;
            monitorBgColor = settingAsset.monitorBgColor;
            avatarMonitorField.SetAvatarCamBgColor(monitorBgColor);

            sizeType = settingAsset.monitorSizeType;
            if (settingAsset.monitorSizeType != MonitorSize.Custom)
            {
                monitorSize = (int)settingAsset.monitorSizeType;
            }
            else
            {
                monitorSize = (int)settingAsset.monitorSize;
            }
        }

        public void SaveSettingData(ref SettingData settingAsset)
        {
            settingAsset.defaultZoomDist = defaultZoomDist;
            settingAsset.faceZoomDist = faceZoomDist;
            settingAsset.zoomStepDist = zoomStepDist;

            settingAsset.isGammaCorrection = isGammaCorrection;
            settingAsset.monitorBgColor = monitorBgColor;

            settingAsset.monitorSizeType = sizeType;
            if (sizeType == MonitorSize.Custom)
            {
                settingAsset.monitorSize = monitorSize;
            }
        }
    }
}


