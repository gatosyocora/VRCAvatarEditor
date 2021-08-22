using System.Collections.Generic;
using UnityEngine;
using VRCAvatarEditor.Base;
using VRCAvatar = VRCAvatarEditor.Base.VRCAvatarBase;

namespace VRCAvatarEditor.Interfaces
{
    public interface IFaceEmotion
    {
        AnimationClip CreateBlendShapeAnimationClip(string fileName, string saveFolderPath, VRCAvatar avatar);
        
        void ResetAllBlendShapeValues(VRCAvatar avatar);
        bool SetBlendShapeMaxValue(SkinnedMeshRenderer renderer, int id);
        bool SetBlendShapeMinValue(SkinnedMeshRenderer renderer, int id);
        bool SetContainsAll(bool value, List<SkinnedMesh.BlendShape> blendshapes);
        
        void LoadAnimationProperties(FaceEmotionViewBase faceEmotionGUI, VRCAvatarEditorView editorGUI);
        void ApplyAnimationProperties(List<AnimParam> animProperties, VRCAvatar avatar);
        void ApplyAnimationProperties(AnimationClip clip, VRCAvatar avatar);

        void SetToDefaultFaceEmotion(VRCAvatar editAvatar, VRCAvatar originalAvatar);
        void ResetToDefaultFaceEmotion(VRCAvatar avatar);
        
        List<AnimParam> GetAnimationParamaters(AnimationClip clip);
        List<AnimParam> GetAvatarFaceParamaters(List<SkinnedMesh> skinnedMeshList);
        
        void CopyAnimationKeysFromOriginClip(AnimationClip originClip, AnimationClip targetClip);
    }
}
