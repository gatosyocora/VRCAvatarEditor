using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Avatar = VRCAvatarEditor.VRCAvatar;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class FaceEmotion
    {
        public static readonly string SENDDATAASSET_PATH = "Assets/SendData.asset";

        public class AnimParam
        {
            public string objPath;
            public string blendShapeName;
            public float value;
            public bool isSelect;

            public AnimParam(string path, string propertyName, float value)
            {
                objPath = path;
                this.blendShapeName = propertyName.Replace("blendShape.", "");
                this.value = value;
                isSelect = true;
            }
        }

        /// <summary>
        /// 指定したBlendShapeのアニメーションファイルを作成する
        /// </summary>
        public static AnimationClip CreateBlendShapeAnimationClip(string fileName, string saveFolderPath, ref VRCAvatar avatar, ref List<string> exclusions, GameObject rootObj)
        {
            AnimationClip animClip = new AnimationClip();

            if (fileName == "") fileName = "face_emotion";

            foreach (var skinnedMesh in avatar.SkinnedMeshList)
            {
                if (!skinnedMesh.IsOpenBlendShapes) continue;

                string path = GetHierarchyPath(rootObj, skinnedMesh.Renderer.gameObject);

                foreach (var blendshape in skinnedMesh.Blendshapes)
                {
                    float keyValue;
                    AnimationCurve curve = new AnimationCurve();
                    if (!blendshape.IsExclusion && blendshape.IsContains)
                    {
                        keyValue = skinnedMesh.Renderer.GetBlendShapeWeight(blendshape.Id);

                        curve.keys = null;

                        curve.AddKey(0, keyValue);
                        curve.AddKey(1 / 60.0f, keyValue);

                        animClip.SetCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + blendshape.Name, curve);

                    }
                }
            }

            AssetDatabase.CreateAsset(animClip, AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + fileName + ".anim"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return animClip;
        }

        /// <summary>
        /// 特定のオブジェクトまでのパスを取得する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GetHierarchyPath(GameObject rootObj, GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.parent == null || parent.gameObject == rootObj) return path;

                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// 指定オブジェクト以下でBlendShapeがついているSkinnedMeshRendererのリストを取得する
        /// </summary>
        /// <param name="parentObj">親オブジェクト</param>
        /// <returns>BlendShapeがついているSkinnedMeshRendererのリスト</returns>
        public static List<SkinnedMesh> GetSkinnedMeshListOfBlendShape(GameObject parentObj, GameObject faceMeshObj)
        {
            var skinnedMeshList = new List<SkinnedMesh>();

            var skinnedMeshes = parentObj.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var skinnedMesh in skinnedMeshes)
            {
                if (skinnedMesh is null || skinnedMesh.sharedMesh is null) continue;

                if (skinnedMesh.sharedMesh.blendShapeCount > 0)
                    skinnedMeshList.Add(new SkinnedMesh(skinnedMesh, faceMeshObj));
            }

            return skinnedMeshList;
        }

        /// <summary>
        /// BlendShapeの値をすべてリセットする
        /// </summary>
        public static void ResetAllBlendShapeValues(ref VRCAvatar avatar)
        {
            foreach (var skinnedMesh in avatar.SkinnedMeshList)
            {
                if (!skinnedMesh.IsOpenBlendShapes) continue;

                foreach (var blendshape in skinnedMesh.Blendshapes)
                    if (blendshape.IsContains)
                        SetBlendShapeMinValue(skinnedMesh.Renderer, blendshape.Id);
            }
        }

        /// <summary>
        /// BlendShapeの値を最大値にする
        /// </summary>
        public static bool SetBlendShapeMaxValue(SkinnedMeshRenderer renderer, int id)
        {
            float maxValue = 0f;

            if (renderer.sharedMesh != null)
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null) return false;

                for (int frameIndex = 0; frameIndex < mesh.GetBlendShapeFrameCount(id); frameIndex++)
                    maxValue = Mathf.Max(maxValue, mesh.GetBlendShapeFrameWeight(id, frameIndex));

                renderer.SetBlendShapeWeight(id, maxValue);

                return true;
            }

            return false;
        }

        /// <summary>
        /// BlendShapeの値を最小値にする
        public static bool SetBlendShapeMinValue(SkinnedMeshRenderer renderer, int id)
        {
            float minValue = 0f;

            if (renderer.sharedMesh != null)
            {
                var mesh = renderer.sharedMesh;
                if (mesh == null) return false;

                for (int frameIndex = 0; frameIndex < mesh.GetBlendShapeFrameCount(id); frameIndex++)
                    minValue = Mathf.Min(minValue, mesh.GetBlendShapeFrameWeight(id, frameIndex));

                renderer.SetBlendShapeWeight(id, minValue);

                return true;
            }
            return false;
        }

        /// <summary>
        /// すべてのBlendshapeのisContainの値を変更する
        /// </summary>
        /// <param name="value"></param>
        /// <param name="blendshapes"></param>
        public static bool SetContainsAll(bool value, List<SkinnedMesh.BlendShape> blendshapes)
        {
            if (blendshapes == null) return false;

            foreach (var blendshape in blendshapes)
                blendshape.IsContains = value;

            return true;
        }

        /// <summary>
        /// アニメーションファイルを選択し, BlendShapeのデータを読み込む
        /// 読み込んだデータはScriptableSingleton<SendData>に格納される
        /// </summary>
        /// <param name="sendData"></param>
        /// <param name="preWindow"></param>
        public static void LoadAnimationProperties(FaceEmotionGUI faceEmotionGUI, VRCAvatarEditorGUI editorGUI)
        {

            string animFilePath = EditorUtility.OpenFilePanel("Select Loading Animation File", "Assets", "anim");

            if (animFilePath == "") return;

            animFilePath = FileUtil.GetProjectRelativePath(animFilePath);

            ScriptableSingleton<SendData>.instance.filePath = animFilePath;

            AnimationLoaderGUI.OnLoadedAnimationProperties -= faceEmotionGUI.OnLoadedAnimationProperties;
            AnimationLoaderGUI.OnLoadedAnimationProperties += faceEmotionGUI.OnLoadedAnimationProperties;
            editorGUI.OpenSubWindow();
        }

        /// <summary>
        /// 選択したblendshapeのプロパティを反映する
        /// </summary>
        /// <param name="animProperties"></param>
        /// <param name="skinnedMeshes"></param>
        public static void ApplyAnimationProperties(List<AnimParam> animProperties, ref VRCAvatar avatar)
        {
            for (int skinnedMeshIndex = 0; skinnedMeshIndex < avatar.SkinnedMeshList.Count; skinnedMeshIndex++)
            {
                var mesh = avatar.SkinnedMeshList[skinnedMeshIndex].Mesh;
                var renderer = avatar.SkinnedMeshList[skinnedMeshIndex].Renderer;
                var blendshapes = avatar.SkinnedMeshList[skinnedMeshIndex].Blendshapes;

                if (renderer == null) continue;

                // 一旦すべてリセットする
                for (int i = 0; i < avatar.DefaultFaceEmotion.Count; i++)
                {
                    var defaultAnimProperty = avatar.DefaultFaceEmotion[i];
                    var index = mesh.GetBlendShapeIndex(defaultAnimProperty.blendShapeName);
                    if (index < 0) continue;
                    renderer.SetBlendShapeWeight(index, defaultAnimProperty.value);
                    blendshapes[index].IsContains = false;
                }

                // アニメーションファイルに含まれるものだけ値を変更して
                // 適用させるチェックマークにチェックをいれる
                foreach (var animProperty in animProperties)
                {
                    var index = mesh.GetBlendShapeIndex(animProperty.blendShapeName);
                    if (index >= 0)
                    {
                        renderer.SetBlendShapeWeight(index, animProperty.value);
                        blendshapes[index].IsContains = true;
                    }
                }
            }
        }

        public static void ApplyAnimationProperties(AnimationClip clip, ref VRCAvatar avatar)
        {
            var paramList = GetAnimationParamaters(clip);
            ApplyAnimationProperties(paramList, ref avatar);
        }

        public static void SetToDefaultFaceEmotion(ref VRCAvatar editAvatar, VRCAvatar originalAvatar)
        {
            var defaultFaceEmotion = GetAvatarFaceParamaters(editAvatar.SkinnedMeshList);
            editAvatar.DefaultFaceEmotion = defaultFaceEmotion;
            ApplyAnimationProperties(defaultFaceEmotion, ref originalAvatar);
        }

        public static void ResetToDefaultFaceEmotion(ref VRCAvatar avatar)
        {
            if (avatar == null) return;

            ApplyAnimationProperties(avatar.DefaultFaceEmotion, ref avatar);
        }

        public static List<AnimParam> GetAnimationParamaters(AnimationClip clip)
        {
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var animParamList = new List<AnimParam>();

            foreach (var binding in bindings)
            {
                if ((binding.propertyName).Split('.')[0] != "blendShape") continue;
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var animParam = new AnimParam(binding.path, binding.propertyName, curve[0].value);
                animParamList.Add(animParam);
            }

            return animParamList;
        }

        public static List<AnimParam> GetAvatarFaceParamaters(List<SkinnedMesh> skinnedMeshList)
        {
            var animParamList = new List<AnimParam>();

            for (int skinnedMeshIndex = 0; skinnedMeshIndex < skinnedMeshList.Count; skinnedMeshIndex++)
            {
                var mesh = skinnedMeshList[skinnedMeshIndex].Mesh;
                var renderer = skinnedMeshList[skinnedMeshIndex].Renderer;

                for (int blendShapeIndex = 0; blendShapeIndex < mesh.blendShapeCount; blendShapeIndex++)
                {
                    var blendShapeName = mesh.GetBlendShapeName(blendShapeIndex);
                    var weight = renderer.GetBlendShapeWeight(blendShapeIndex);

                    var animParam = new AnimParam("", blendShapeName, weight);
                    animParamList.Add(animParam);
                }
            }

            return animParamList;
        }

        /// <summary>
        /// originClipに設定されたAnimationKeyをすべてtargetclipにコピーする
        /// </summary>
        public static void CopyAnimationKeysFromOriginClip(AnimationClip originClip, AnimationClip targetClip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(originClip))
                AnimationUtility.SetEditorCurve(targetClip, binding, AnimationUtility.GetEditorCurve(originClip, binding));
        }
    }
}
