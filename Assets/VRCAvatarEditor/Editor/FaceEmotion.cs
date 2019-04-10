using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class FaceEmotion
    {
        private static readonly string[] HANDANIMS = { "FIST", "FINGERPOINT", "ROCKNROLL", "HANDOPEN", "THUMBSUP", "VICTORY", "HANDGUN" };


        /// <summary>
        /// 表情用のAnimationClipを作成する
        /// </summary>
        public static void CreateAndSetAnimClip(string animFileName, string saveFolderPath, List<SkinnedMesh> skinnedMeshList, ref AnimatorOverrideController controller, HandPose.HandPoseType selectedHandAnim, List<string> exclusions)
        {
            var animClip = CreateBlendShapeAnimationClip(animFileName, saveFolderPath, skinnedMeshList, selectedHandAnim, exclusions);

            if (selectedHandAnim != HandPose.HandPoseType.None)
                controller[HANDANIMS[(int)selectedHandAnim-1]] = animClip;
        }

        /// <summary>
        /// 指定したBlendShapeのアニメーションファイルを作成する
        /// </summary>
        public static AnimationClip CreateBlendShapeAnimationClip(string fileName, string saveFolderPath, List<SkinnedMesh> skinnedMeshList, HandPose.HandPoseType selectedHandAnim, List<string> exclusions)
        {
            AnimationClip animClip = new AnimationClip();

            if (fileName == "") fileName = "face_emotion";

            foreach (var skinnedMesh in skinnedMeshList)
            {
                if (!skinnedMesh.isOpenBlendShapes) continue;

                string path = GetHierarchyPath(skinnedMesh.renderer.gameObject);

                for (int i = 0; i < skinnedMesh.blendShapeNum; i++)
                {
                    var blendshape = skinnedMesh.blendshapes[i];

                    if (!blendshape.isExclusion && blendshape.isContains)
                    {

                        float keyValue = skinnedMesh.renderer.GetBlendShapeWeight(blendshape.id);

                        Keyframe startKeyframe = new Keyframe(0, keyValue);
                        Keyframe endKeyframe = new Keyframe(1 / 60.0f, keyValue);

                        AnimationCurve curve = new AnimationCurve();

                        curve.AddKey(startKeyframe);
                        curve.AddKey(endKeyframe);

                        animClip.SetCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + blendshape.name, curve);
                    }

                }
            }

            // 特定の手の形を追加する
            if (selectedHandAnim != HandPose.HandPoseType.None)
                HandPose.AddHandPoseAnimationKeys(animClip, selectedHandAnim);

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
        public static string GetHierarchyPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.parent == null) return path;

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
        public static List<SkinnedMesh> GetSkinnedMeshListOfBlendShape(GameObject parentObj)
        {
            var skinnedMeshList = new List<SkinnedMesh>();

            var skinnedMeshes = parentObj.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var skinnedMesh in skinnedMeshes)
            {
                var blendShapeNum = skinnedMesh.sharedMesh.blendShapeCount;
                if (blendShapeNum > 0)
                    skinnedMeshList.Add(new SkinnedMesh(skinnedMesh));
            }

            return skinnedMeshList;
        }

        /// <summary>
        /// BlendShapeをすべてリセットする
        /// </summary>
        public static void ResetBlendShapes(ref List<SkinnedMesh> skinnedMeshList)
        {
            foreach (var skinnedMesh in skinnedMeshList)
            {
                if (!skinnedMesh.isOpenBlendShapes) continue;

                foreach (var blendshape in skinnedMesh.blendshapes)
                {
                    if (blendshape.isContains)
                    {
                        SetBlendShapeMinValue(ref skinnedMesh.renderer, blendshape.id);
                    }
                }
            }
        }

        /// <summary>
        /// BlendShapeの値を最大値にする
        /// </summary>
        public static void SetBlendShapeMaxValue(ref SkinnedMeshRenderer renderer, int id)
        {
            float maxValue = 1f;

            if (renderer.sharedMesh != null)
            {
                var mesh = renderer.sharedMesh;

                var frameNum = mesh.GetBlendShapeFrameCount(id);

                for (int i = 0; i < frameNum; i++)
                {
                    var frameWeight = mesh.GetBlendShapeFrameWeight(id, i);
                    maxValue = Mathf.Max(maxValue, frameWeight);
                }

                renderer.SetBlendShapeWeight(id, maxValue);
            }
        }

        /// <summary>
        /// BlendShapeの値を最小値にする
        /// </summary>
        public static void SetBlendShapeMinValue(ref SkinnedMeshRenderer renderer, int id)
        {
            float minValue = 0f;

            if (renderer.sharedMesh != null)
            {
                var mesh = renderer.sharedMesh;

                var frameNum = mesh.GetBlendShapeFrameCount(id);

                for (int i = 0; i < frameNum; i++)
                {
                    var frameWeight = mesh.GetBlendShapeFrameWeight(id, i);
                    minValue = Mathf.Min(minValue, frameWeight);
                }

                renderer.SetBlendShapeWeight(id, minValue);
            }
        }

        /// <summary>
        /// すべてのBlendshapeのisContainの値を変更する
        /// </summary>
        /// <param name="value"></param>
        /// <param name="blendshapes"></param>
        public static void SetContainsAll(bool value, ref List<SkinnedMesh.BlendShape> blendshapes)
        {
            if (blendshapes == null) return;

            foreach (var blendshape in blendshapes)
                blendshape.isContains = value;
        }
    }
}

