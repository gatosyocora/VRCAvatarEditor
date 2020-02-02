using UnityEngine;
using UnityEditor;

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
        
        private float cameraHeight = 1;
        private float maxCamHeight = 1;
        private float minCamHeight = 0;
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

        public void Initialize(ref VRCAvatarEditor.Avatar avatar, VRCAvatarEditorGUI.ToolFunc currentTool)
        {
            this.avatar = avatar;
            this.currentTool = currentTool;

            upDownTexture = Resources.Load<Texture>("Icon/UpDown");
            avatarCamTexture = Resources.Load<RenderTexture>("AvatarRT");
            gammaMat = Resources.Load<Material>("Gamma");

            sceneLight = GetDirectionalLight();
        }

        public void Dispose()
        {
            if (avatarCam != null)
                UnityEngine.Object.DestroyImmediate(avatarCam);
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
                        int eventType = 0;
                        var delta = GatoGUILayout.MiniMonitor(avatarCamTexture, monitorSize, monitorSize, ref eventType, isGammaCorrection, gammaMat);
                        if (!isLightPressing)
                        {
                            if (delta != Vector2.zero)
                            {
                                if (eventType == (int)EventType.MouseDrag) RotateAvatarCam(delta);
                                else if (eventType == (int)EventType.ScrollWheel) ZoomAvatarCam(delta, zoomLevel);
                                return true;
                            }
                        }

                        GUILayout.FlexibleSpace();
                    }

                    // アバター回転
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("<"))
                        {
                            if (avatarCam != null)
                                avatarCam.transform.Rotate(0, -CAMERA_ROTATE_ANGLE, 0);
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
                                avatarCam.transform.Rotate(0, CAMERA_ROTATE_ANGLE, 0);
                        }
                    }

                    using (new GUILayout.HorizontalScope())
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        zoomLevel = EditorGUILayout.Slider(zoomLevel, 0f, 1f);

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
                if (check.changed) SetAvatarCamBgColor(monitorBgColor);
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
        /// AvatarCamの位置を動かす
        /// </summary>
        public void MoveAvatarCam()
        {
            if (avatarCam == null || avatar.descriptor == null) return;

            var nowPos = avatarCam.transform.position;
            var avatarPos = avatar.descriptor.transform.position;
            var childTrans = avatarCam.transform.Find("Main").gameObject.transform;

            // 顔にあわせる
            if (this.currentTool == VRCAvatarEditorGUI.ToolFunc.表情設定)
            {
                cameraHeight = avatar.eyePos.y;
                avatarCam.transform.position = new Vector3(nowPos.x, cameraHeight + avatarPos.y, nowPos.z);
                childTrans.localPosition = new Vector3(0, 0, faceZoomDist);
                camPosZ = faceZoomDist;
            }
            else
            {
                cameraHeight = (maxCamHeight > 1) ? 1 : maxCamHeight;
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
            if (avatarCam == null || avatar.descriptor == null) return;
            var nowPos = avatarCam.transform.position;
            var avatarPos = avatar.descriptor.transform.position;
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
            cam.transform.Translate(new Vector3(0, 0, (float)(-(delta.y / Mathf.Abs(delta.y)) * zoomStepDist)));
            camPosZ = cam.transform.localPosition.z + zoomStepDist * (1 - level);
        }

        /// <summary>
        /// AvatarCamをズームさせる（スライダー）
        /// </summary>
        /// <param name="delta"></param>
        private void ZoomAvatarCam(float level)
        {
            if (avatarCam == null) return;
            var cam = avatarCam.transform.GetChild(0);
            cam.transform.localPosition = new Vector3(0, 0, camPosZ - zoomStepDist * (1 - level));
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
            Repaint();
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
        /// アバターを写す用のカメラを設定する
        /// </summary>
        public void SetAvatarCam(GameObject obj)
        {
            if (avatarCam != null)
                DestroyImmediate(avatarCam);

            var avatarCam_prefab = Resources.Load<GameObject>("AvatarCam");
            avatarCam = PrefabUtility.InstantiatePrefab(avatarCam_prefab) as GameObject;
            avatarCam.transform.position = obj.transform.position;

            maxCamHeight = avatar.eyePos.y;

            SetAvatarCamBgColor(monitorBgColor);

            MoveAvatarCam();
        }

        public void LoadSettingData(SettingData settingAsset)
        {
            defaultZoomDist = settingAsset.defaultZoomDist;
            faceZoomDist = settingAsset.faceZoomDist;
            zoomStepDist = settingAsset.zoomStepDist;

            isGammaCorrection = settingAsset.isGammaCorrection;
            monitorBgColor = settingAsset.monitorBgColor;
            SetAvatarCamBgColor(monitorBgColor);

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

        public void ChangeTab(VRCAvatarEditorGUI.ToolFunc tool)
        {
            this.currentTool = tool;
        }
    }
}


