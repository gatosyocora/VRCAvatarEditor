using UnityEngine;
using UnityEditor;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class HandPose
    {
        public static readonly string[] HAND_ORIGIN_ANIM_FILE_NAMES = { "Fist", "FingerPoint", "RocknRoll", "HandOpen", "ThumbsUp", "Victory", "HandGun" };

        public enum HandPoseType
        {
            None,
            FIST,
            FINGERPOINT,
            ROCKNROLL,
            HANDOPEN,
            THUMBSUP,
            VICTORY,
            HANDGUN,
        }

        /// <summary>
        /// 特定のAnimationファイルのAnimationキー全てをコピーする
        /// </summary>
        public static bool AddHandPoseAnimationKeysFromOriginClip(ref AnimationClip targetClip, HandPoseType originHandType)
        {
            if (originHandType == HandPoseType.None) return false;

            string handPoseAnimPath = "HandPoseAnimation/"+HAND_ORIGIN_ANIM_FILE_NAMES[(int)originHandType-1];
            AnimationClip originClip = Resources.Load<AnimationClip>(handPoseAnimPath);

            CopyAnimationKeysFromOriginClip(originClip, targetClip);

            return true;
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
