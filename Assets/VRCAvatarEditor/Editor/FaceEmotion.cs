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

            bool isExclusionKey;

            foreach (var skinnedMesh in skinnedMeshList)
            {
                if (!skinnedMesh.isOpenBlendShapes) continue;

                string path = GetHierarchyPath(skinnedMesh.renderer.gameObject);

                for (int i = 0; i < skinnedMesh.blendShapeNum; i++)
                {
                    isExclusionKey = false;

                    AnimationCurve curve = new AnimationCurve();

                    float keyValue = skinnedMesh.renderer.GetBlendShapeWeight(i);

                    // 除外するキーかどうか調べる
                    foreach (var exclusionWord in exclusions)
                    {
                        if (exclusionWord == "" || isExclusionKey) continue;
                        if (skinnedMesh.blendshapes[i].Contains(exclusionWord))
                            isExclusionKey = true;
                    }

                    if (!isExclusionKey)
                    {
                        //if (keyValue <= 0) continue; // 0のキーは追加しない

                        Keyframe startKeyframe = new Keyframe(0, keyValue);
                        Keyframe endKeyframe = new Keyframe(1 / 60.0f, keyValue);

                        curve.AddKey(startKeyframe);
                        curve.AddKey(endKeyframe);

                        animClip.SetCurve(path, typeof(SkinnedMeshRenderer), "blendShape." + skinnedMesh.blendshapes[i], curve);
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

                for (int i = 0; i < skinnedMesh.blendShapeNum; i++)
                {
                    skinnedMesh.renderer.SetBlendShapeWeight(i, 0);
                }
            }
        }
    }
}

