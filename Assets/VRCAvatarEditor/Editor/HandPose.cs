using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class HandPose
    {

        private static string ORIGIN_ANIM_PATH = "Assets/VRCAvatarEditor/Origins/"; // コピー元となるAnimationファイルが置いてあるディレクトリのパス

        private static readonly string[] HANDNAMES ={"LeftHand.Index", "LeftHand.Little", "LeftHand.Middle", "LeftHand.Ring", "LeftHand.Thumb",
                                                 "RightHand.Index", "RightHand.Little", "RightHand.Middle", "RightHand.Ring", "RightHand.Thumb"};

        public static readonly string[] HAND_ORIGIN_ANIM_FILE_NAMES = { "Fist", "FingerPoint", "RocknRoll", "HandOpen", "ThumbsUp", "Victory", "HandGun" };

        private static readonly string[] HANDPOSTYPES = { "1 Stretched", "2 Stretched", "3 Stretched", "Spread" };

        public enum HandPoseType
        {
            None,
            FIST,
            FINGERPOINT,
            ROKUNROLL,
            HANDOPEN,
            THUMBSUP,
            VICTORY,
            HANDGUN,
        }

        /// <summary>
        /// 特定のAnimationファイルのAnimationキー全てをコピーする
        /// </summary>
        public static void AddHandPoseAnimationKeys(AnimationClip targetClip, HandPoseType originHandType)
        {
            if (originHandType == HandPoseType.None) return;

            string originPath = ORIGIN_ANIM_PATH + HAND_ORIGIN_ANIM_FILE_NAMES[(int)originHandType-1] + ".anim";
            AnimationClip originClip = (AnimationClip)AssetDatabase.LoadAssetAtPath(originPath, typeof(AnimationClip));         // originPathよりAnimationClipの読み込み

            CopyAnimationKeys(originClip, targetClip);
        }

        /// <summary>
        /// originClipに設定されたAnimationKeyをすべてtargetclipにコピーする
        /// </summary>
        public static void CopyAnimationKeys(AnimationClip originClip, AnimationClip targetClip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(originClip).ToArray())
            {
                // AnimationClipよりAnimationCurveを取得
                AnimationCurve curve = AnimationUtility.GetEditorCurve(originClip, binding);
                // AnimationClipにキーリダクションを行ったAnimationCurveを設定
                AnimationUtility.SetEditorCurve(targetClip, binding, curve);
            }
        }

        /// <summary>
        /// 手の形に関するAnimationキーを全て削除する
        /// </summary>
        /// <param name="command"></param>
        public static void ClearHandPoseAnimationKeys(MenuCommand command)
        {
            var targetClip = command.context as AnimationClip;

            foreach (var handname in HANDNAMES)
            {
                foreach (var handpostype in HANDPOSTYPES)
                {
                    var binding = new EditorCurveBinding();
                    binding.path = "";
                    binding.type = typeof(Animator);
                    binding.propertyName = handname + "." + handpostype;

                    // キーを削除する
                    AnimationUtility.SetEditorCurve(targetClip, binding, null);
                }
            }

        }
    }

}
