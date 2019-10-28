using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{

    public class ProbeAnchor
    {

        public enum TARGETPOS
        {
            HEAD,
            CHEST,
            //ARMATURE,
            ROOTOBJECT,
        }

        private const string TARGETOBJNAME = "Anchor Target";

        /// <summary>
        /// 未作成であればAnchorTargetに設定用のオブジェクトを作成し, 
        /// targetPosに設定する
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="targetPos"></param>
        /// <param name="anchorTargetObj"></param>
        /// <returns></returns>
        public static bool CreateAndSetProbeAnchorObject(GameObject obj, TARGETPOS targetPos, ref GameObject anchorTargetObj)
        {
            var animator = obj.GetComponent<Animator>();
            if (animator == null) return false;

            // AnchorTargetを設定する基準の場所を取得
            Transform targetPosTrans = null;
            if (targetPos == TARGETPOS.HEAD)
            {
                targetPosTrans = animator.GetBoneTransform(HumanBodyBones.Head);
            }
            else if (targetPos == TARGETPOS.CHEST)
            {
                targetPosTrans = animator.GetBoneTransform(HumanBodyBones.Chest);
            }
            /*else if (targetPos == TARGETPOS.ARMATURE)
            {
                var hipsTrans = animator.GetBoneTransform(HumanBodyBones.Hips);
                targetPosTrans = hipsTrans.parent;
            }*/
            else if (targetPos == TARGETPOS.ROOTOBJECT)
            {
                targetPosTrans = obj.transform;
            }
            else
                return false;

            // AnchorTargetに設定用のオブジェクトを作成
            anchorTargetObj = GameObject.Find(obj.name + "/" + TARGETOBJNAME);
            if (anchorTargetObj == null)
            {
                anchorTargetObj = new GameObject(TARGETOBJNAME);
                anchorTargetObj.transform.SetParent(obj.transform);
            }
            anchorTargetObj.transform.position = targetPosTrans.position;

            return true;
        }

        public static void SetProbeAnchorToSkinnedMeshRenderers(ref GameObject anchorTargetObj, ref VRCAvatarEditor.Avatar avatar, ref bool[] isSettingToSkinnedMesh)
        {
            List<SkinnedMeshRenderer> skinnedMeshRendererList = avatar.skinnedMeshRendererList;

            for (int index = 0; index < skinnedMeshRendererList.Count; index++)
            {
                if (isSettingToSkinnedMesh[index])
                    skinnedMeshRendererList[index].probeAnchor = anchorTargetObj.transform;
                else
                    skinnedMeshRendererList[index].probeAnchor = null;
            }
        }

        public static void SetProbeAnchorToMeshRenderers(ref GameObject anchorTargetObj, ref VRCAvatarEditor.Avatar avatar, ref bool[] isSettingToMesh)
        {
            List<MeshRenderer> meshRendererList = avatar.meshRendererList;

            for (int index = 0; index < meshRendererList.Count; index++)
            {
                if (isSettingToMesh[index])
                    meshRendererList[index].probeAnchor = anchorTargetObj.transform;
                else
                    meshRendererList[index].probeAnchor = null;
            }
        }

    }

}
