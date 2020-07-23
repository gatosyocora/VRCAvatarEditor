using UnityEditor;
using UnityEngine;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{
    public class HandPose
    {
        public static readonly string[] HAND_ORIGIN_ANIM_FILE_NAMES = { "Fist", "FingerPoint", "RocknRoll", "HandOpen", "ThumbsUp", "Victory", "HandGun" };

        public enum HandPoseType
        {
            NoSelection,
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
            if (originHandType == HandPoseType.NoSelection) return false;

            AnimationClip originClip = GetHandAnimationClip(originHandType);

            CopyAnimationKeysFromOriginClip(originClip, targetClip);

            return true;
        }

        /// <summary>
        /// 特定のAnimationファイルの手に関するAnimationキー全てをコピーする
        /// </summary>
        public static bool AddHandPoseAnimationKeysFromOriginClip(AnimationClip targetClip, AnimationClip handAnimationClip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(handAnimationClip))
            {
                // 手を動かすKey以外は追加しない
                var propertyName = binding.propertyName;
                if (!propertyName.EndsWith("Spread") &&
                    !propertyName.EndsWith("Stretched")) continue;

                AnimationUtility.SetEditorCurve(targetClip, binding, AnimationUtility.GetEditorCurve(handAnimationClip, binding));
            }

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

        public static AnimationClip GetHandAnimationClip(HandPoseType originHandType)
        {
            if (originHandType == HandPoseType.NoSelection) return null;

            string handPoseAnimPath = "HandPoseAnimation/" + HAND_ORIGIN_ANIM_FILE_NAMES[(int)originHandType - 1];
            return Resources.Load<AnimationClip>(handPoseAnimPath);
        }
    }

}
