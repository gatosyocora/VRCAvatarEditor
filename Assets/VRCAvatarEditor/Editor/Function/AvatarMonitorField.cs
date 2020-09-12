using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if VRC_SDK_VRCSDK2
using VRCSDK2;
using VRCAvatar = VRCAvatarEditor.Avatars2.VRCAvatar2;
#else
using VRCAvatar = VRCAvatarEditor.Test.VRCAvatar2;
using VRC_AvatarDescriptor = VRC.SDKBase.VRC_AvatarDescriptor;
#endif

namespace VRCAvatarEditor
{
    public class AvatarMonitorField : IDisposable
    {
        private Scene scene;
        private GameObject cameraObj;
        private GameObject avatarObj;
        private GameObject lightObj;
        private Camera camera;
        private RenderTexture renderTexture;
        private int monitorSize;
        private Material textureMat;

        private VRC_AvatarDescriptor descriptor;

        public VRCAvatar avatar { get; private set; }
        private SkinnedMeshRenderer faceMesh;

        private Rect rect;

        private float mainOrthographicSize;
        private float subOrthographicSize;
        private float defaultOrthographicSize = 0.5f;
        private float faceOrthographicSize = 0.1f;
        private float orthographicsStep = 0.1f;

        public AvatarMonitorField()
        {
            this.monitorSize = 0;
            this.textureMat = CreateGammaMaterial();

            this.mainOrthographicSize = defaultOrthographicSize;
            this.subOrthographicSize = 0f;

            scene = EditorSceneManager.NewPreviewScene();

            cameraObj = CreateCameraObj();
            AddGameObject(cameraObj);
            SetAvatarCamBgColor(Color.black);

            lightObj = CreateLightObj();
            AddGameObject(lightObj);
        }


        public void Initinalize(VRCAvatar avatar)
        {

        }

        public bool Render(int monitorSize, bool isGammaCorrection = true)
        {
            if (monitorSize != this.monitorSize || renderTexture == null)
            {
                ResizeMonitor(monitorSize);
            }

            rect = GUILayoutUtility.GetRect(monitorSize, monitorSize);

            var e = Event.current;

            if (rect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDrag)
                {
                    RotateAvatarCam(e.delta);
                    return true;
                }
                else if (e.type == EventType.ScrollWheel)
                {
                    ZoomAvatarCam(e.delta);
                    return true;
                }
            }

            var oldAllowPipes = Unsupported.useScriptableRenderPipeline;
            Unsupported.useScriptableRenderPipeline = false;
            camera.Render();
            Unsupported.useScriptableRenderPipeline = oldAllowPipes;

            // TODO: Editorを開いた状態で再生すると再生後にAvatarMonitorのガンマ補正がなくなる
            // Disposeによって？textureMatがnullになっているので再生成する
            // もっといい解決策がありそう
            if (textureMat == null)
            {
                textureMat = CreateGammaMaterial();
            }

            Graphics.DrawTexture(rect, renderTexture, (isGammaCorrection) ? textureMat : null);

            return false;
        }

        public void AddGameObject(GameObject obj)
        {
            SceneManager.MoveGameObjectToScene(obj, scene);
        }

        private GameObject CreateCameraObj()
        {
            var cameraObj = new GameObject("Camera", typeof(Camera));
            camera = cameraObj.GetComponent<Camera>();
            camera.cameraType = CameraType.Preview;
            camera.orthographic = true;
            camera.orthographicSize = defaultOrthographicSize;
            camera.forceIntoRenderTexture = true;
            camera.scene = scene;
            camera.enabled = false;
            camera.nearClipPlane = 0.01f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            return cameraObj;
        }

        private GameObject CreateLightObj()
        {
            var lightObj = new GameObject("Directional Light", typeof(Light));
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
            var light = lightObj.GetComponent<Light>();
            light.type = LightType.Directional;
            return lightObj;
        }

        public VRCAvatar AddAvatar(VRC_AvatarDescriptor descriptor)
        {
#if VRC_SDK_VRCSDK2
            if (avatarObj != null)
                UnityEngine.Object.DestroyImmediate(avatarObj);

            var newAvatarObj = GameObject.Instantiate(descriptor.gameObject);
            newAvatarObj.transform.position = Vector3.zero;
            newAvatarObj.transform.rotation = Quaternion.identity;
            newAvatarObj.SetActive(true);
            AddGameObject(newAvatarObj);
            this.avatarObj = newAvatarObj;
            newAvatarObj.transform.position = new Vector3(0, 0, 0);
            this.descriptor = newAvatarObj.GetComponent<VRC_AvatarDescriptor>();
            avatar = new VRCAvatar(this.descriptor);
            ResetCameraTransform();

            return avatar;
#else
            return new VRCAvatar();
#endif
        }

        public void SetAvatarCamBgColor(Color col)
        {
            camera.backgroundColor = col;
        }

        /// <summary>
        /// AvatarCamを回転させる
        /// </summary>
        /// <param name="delta"></param>
        private void RotateAvatarCam(Vector2 delta)
        {
            if (camera == null || delta == Vector2.zero) return;
            camera.transform.RotateAround(avatarObj.transform.position, Vector3.up, delta.x);
            //camera.transform.RotateAround(avatarObj.transform.position, Vector3.right, delta.y);
        }

        /// <summary>
        /// AvatarCamをズームさせる(マウスホイール)
        /// </summary>
        /// <param name="delta"></param>
        private void ZoomAvatarCam(Vector2 delta)
        {
            if (camera == null || delta == Vector2.zero) return;

            var newMainOrthographicsSize = mainOrthographicSize + (delta.y / Mathf.Abs(delta.y)) * orthographicsStep;
            if (newMainOrthographicsSize - orthographicsStep > 0)
            {
                camera.orthographicSize = newMainOrthographicsSize - subOrthographicSize;
                mainOrthographicSize = newMainOrthographicsSize;
            }
        }

        /// <summary>
        /// AvatarCamをズームさせる（スライダー）
        /// </summary>
        /// <param name="delta"></param>
        public void ZoomAvatarCam(float level)
        {
            if (camera == null) return;
            var newSubOrthographicsSize = orthographicsStep * level;
            camera.orthographicSize = mainOrthographicSize - newSubOrthographicsSize;
            subOrthographicSize = newSubOrthographicsSize;
        }

        /// <summary>
        /// AvatarCamの位置を動かす
        /// </summary>
        public void MoveAvatarCam(bool setToFace)
        {
            if (camera == null || descriptor == null) return;

            // 顔にあわせる
            if (setToFace)
            {
                mainOrthographicSize = faceOrthographicSize;
            }
            else
            {
                mainOrthographicSize = defaultOrthographicSize;
            }
#if VRC_SDK_VRCSDK2
            camera.transform.position = new Vector3(0, descriptor.ViewPosition.y, 1);
#else
            camera.transform.position = new Vector3(0, 1, 1);
#endif
            cameraObj.transform.rotation = Quaternion.Euler(0, 180, 0);
            subOrthographicSize = 0f;
            camera.orthographicSize = mainOrthographicSize;
        }

        public void ResetCameraTransform()
        {
#if VRC_SDK_VRCSDK2
            cameraObj.transform.position = new Vector3(0, descriptor.ViewPosition.y, 1);
#else
            camera.transform.position = new Vector3(0, 1, 1);
#endif
            cameraObj.transform.rotation = Quaternion.Euler(0, 180, 0);
            mainOrthographicSize = defaultOrthographicSize;
            subOrthographicSize = 0f;
            camera.orthographicSize = mainOrthographicSize;
        }

        /// <summary>
        /// AvatarCamの高さを変える
        /// </summary>
        /// <param name="value"></param>
        public void MoveAvatarCamHeight(float value)
        {
            if (camera == null || descriptor == null) return;
#if VRC_SDK_VRCSDK2
            var y = Mathf.Lerp(0, descriptor.ViewPosition.y * 1.1f, value);
#else
var y = Mathf.Lerp(0, 1.1f, value);
#endif
            var nowCamPos = camera.transform.position;
            camera.transform.position = new Vector3(nowCamPos.x, y, nowCamPos.z);
        }

        public void RotateCamera(int angleY)
        {
            camera.transform.RotateAround(avatarObj.transform.position, Vector3.up, angleY);
        }

        /// <summary>
        /// DirectionalLightを回転させる
        /// </summary>
        /// <param name="delta"></param>
        public void RotateLight(Vector2 delta)
        {
            if (lightObj == null) return;

            lightObj.transform.Rotate(Vector3.up, -delta.x);
        }

        private Material CreateGammaMaterial()
        {
            Shader gammaShader = Resources.Load<Shader>("Gamma");
            return new Material(gammaShader);
        }

        private void ResizeMonitor(int monitorSize)
        {
            this.monitorSize = monitorSize;

            renderTexture = new RenderTexture(monitorSize, monitorSize, 32);
            camera.targetTexture = renderTexture;
        }

        public float GetNormalizedMonitorHeight()
        {
            if (cameraObj == null || descriptor == null) return 1f;
#if VRC_SDK_VRCSDK2
            return cameraObj.transform.position.y / (descriptor.ViewPosition.y * 1.1f);
#else
return cameraObj.transform.position.y / 1.1f;
#endif
        }

        public float GetNormalizedSubOrthographicSize()
        {
            return subOrthographicSize / orthographicsStep;
        }

        public void SetZoomParameters(float defaultZoomValue, float faceZoomValue, float zoomStepValue)
        {
            defaultOrthographicSize = defaultZoomValue;
            faceOrthographicSize = faceZoomValue;
            orthographicsStep = zoomStepValue;
        }

        public void Dispose()
        {
            camera.targetTexture = null;
            if (renderTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(renderTexture);
                renderTexture = null;
            }

            if (cameraObj != null)
            {
                UnityEngine.Object.DestroyImmediate(cameraObj);
            }
            if (avatarObj != null)
            {
                UnityEngine.Object.DestroyImmediate(avatarObj);
            }
            if (lightObj != null)
            {
                UnityEngine.Object.DestroyImmediate(lightObj);
            }

            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}

