using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VRCSDK2;

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
    public VRCAvatarEditor.Avatar avatar { get; private set; }
    private SkinnedMeshRenderer faceMesh;

    private Rect rect;

    private float zoomLevel = 1.0f;
    private float zoomStepDist = 0.25f;
    private float defaultZoomDist = 1.0f;
    private float faceZoomDist = 0.5f;

    private float defaultOrthographicSize = 0.5f;
    private float faceOrthographicSize = 0.1f;

    public AvatarMonitorField()
    {
        this.monitorSize = 0;
        this.textureMat = CreateGammaMaterial();

        scene = EditorSceneManager.NewPreviewScene();
        cameraObj = new GameObject("Camera", typeof(Camera));
        AddGameObject(cameraObj);
        camera = cameraObj.GetComponent<Camera>();
        camera.cameraType = CameraType.Preview;
        camera.orthographic = true;
        camera.orthographicSize = defaultOrthographicSize;
        camera.forceIntoRenderTexture = true;
        camera.scene = scene;
        camera.enabled = false;
        camera.nearClipPlane = 0.01f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        SetAvatarCamBgColor(Color.black);

        lightObj = new GameObject("Directional Light", typeof(Light));
        lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
        AddGameObject(lightObj);
        var light = lightObj.GetComponent<Light>();
        light.type = LightType.Directional;
    }


    public void Initinalize(VRCAvatarEditor.Avatar avatar)
    {

    }

    public bool Render(int monitorSize, bool isGammaCorrection = true)
    {
        if (monitorSize != this.monitorSize)
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

        Graphics.DrawTexture(rect, renderTexture, (isGammaCorrection) ? textureMat : null);

        return false;
    }

    public void AddGameObject(GameObject obj)
    {
        SceneManager.MoveGameObjectToScene(obj, scene);
    }

    public VRCAvatarEditor.Avatar AddAvatar(VRC_AvatarDescriptor descriptor)
    {
        if (avatarObj != null)
            UnityEngine.Object.DestroyImmediate(avatarObj);

        var newAvatarObj = GameObject.Instantiate(descriptor.gameObject);
        newAvatarObj.SetActive(true);
        AddGameObject(newAvatarObj);
        this.avatarObj = newAvatarObj;
        newAvatarObj.transform.position = new Vector3(0, 0, 0);
        this.descriptor = newAvatarObj.GetComponent<VRC_AvatarDescriptor>();
        avatar = new VRCAvatarEditor.Avatar(this.descriptor);
        ResetCameraTransform();

        return avatar;
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

        var newOrthographicsSize = camera.orthographicSize + (delta.y / Mathf.Abs(delta.y)) * 0.1f;
        if (newOrthographicsSize > 0)
            camera.orthographicSize = newOrthographicsSize;
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
            camera.orthographicSize = faceOrthographicSize;
        }
        else
        {
            camera.orthographicSize = defaultOrthographicSize;
        }
        var nowPos = camera.transform.position;
        camera.transform.position = new Vector3(nowPos.x, descriptor.ViewPosition.y, nowPos.z);
        cameraObj.transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    public void ResetCameraTransform()
    {
        cameraObj.transform.position = new Vector3(0, 1, 1);
        cameraObj.transform.rotation = Quaternion.Euler(0, 180, 0);
    }

    /// <summary>
    /// AvatarCamの高さを変える
    /// </summary>
    /// <param name="value"></param>
    public void MoveAvatarCamHeight(float value)
    {
        if (camera == null || descriptor == null) return;
        var y = Mathf.Lerp(0, descriptor.ViewPosition.y * 1.1f, value);
        var nowCamPos = camera.transform.position;
        camera.transform.position = new Vector3(nowCamPos.x, y, nowCamPos.z);
    }

    /// <summary>
    /// AvatarCamをズームさせる（スライダー）
    /// </summary>
    /// <param name="delta"></param>
    public void ZoomAvatarCam(float level)
    {
        if (camera == null) return;
        camera.transform.Translate(new Vector3(0, 0, -zoomStepDist * (1 - level)));
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
        return cameraObj.transform.position.y / (descriptor.ViewPosition.y * 1.1f);
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
