using System;
using UnityEditor;
using UnityEngine;
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

        private Light sceneLight;
        private bool isLightPressing = false;

        private Texture upDownTexture;
        private Material gammaMat;

        public enum MonitorSize
        {
            Small = 256,
            Mediam = 512,
            Large = 768,
            Custom
        }

        private float defaultZoomDist = 0.5f;
        private float faceZoomDist = 0.1f;
        private float zoomStepDist = 0.1f;

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

                        if (GUILayout.Button(LocalizeText.instance.langPair.reset))
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
                        var zoomLevel = EditorGUILayout.Slider(avatarMonitorField.GetNormalizedSubOrthographicSize(), 0f, 1f);

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
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                defaultZoomDist = EditorGUILayout.FloatField(LocalizeText.instance.langPair.defaultCameraDistanceLabel, defaultZoomDist);
                faceZoomDist = EditorGUILayout.FloatField(LocalizeText.instance.langPair.faceCameraDistanceLabel, faceZoomDist);
                zoomStepDist = EditorGUILayout.FloatField(LocalizeText.instance.langPair.cameraZoomStepDistanceLabel, zoomStepDist);

                if (check.changed)
                    avatarMonitorField.SetZoomParameters(defaultZoomDist, faceZoomDist, zoomStepDist);
            }

            EditorGUILayout.Space();
            isGammaCorrection = EditorGUILayout.ToggleLeft(LocalizeText.instance.langPair.gammaCorrectionLabel, isGammaCorrection);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                monitorBgColor = EditorGUILayout.ColorField(LocalizeText.instance.langPair.monitorBackgroundColorLabel, monitorBgColor);
                if (check.changed) avatarMonitorField.SetAvatarCamBgColor(monitorBgColor);
            }
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                sizeType = (MonitorSize)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.monitorSizeTypeLabel, sizeType);
                if (check.changed && sizeType != MonitorSize.Custom)
                {
                    monitorSize = (int)sizeType;
                }
            }
            using (new EditorGUI.DisabledGroupScope(sizeType != MonitorSize.Custom))
            {
                monitorSize = EditorGUILayout.IntField(LocalizeText.instance.langPair.monitorSizeLabel, monitorSize);
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
            avatarMonitorField.SetZoomParameters(defaultZoomDist, faceZoomDist, zoomStepDist);
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


