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
        /// 特定のオブジェクト以下のRendererのProbeAnchorに設定する
        /// </summary>
        /// <param name="obj"></param>
        public static void SetProbeAnchor(GameObject obj, TARGETPOS targetPos, ref List<SkinnedMeshRenderer> skinnedMeshRendererList, ref List<MeshRenderer> meshRendererList, bool[] isSettingToSkinnedMesh, bool[] isSettingToMesh, bool isGettingSkinnedMeshRenderer, bool isGettingMeshRenderer)
        {
            var animator = obj.GetComponent<Animator>();
            if (animator == null) return;

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
            if (targetPosTrans == null) return;

            // AnchorTargetに設定用のオブジェクトを作成
            GameObject anchorTargetObj = GameObject.Find(obj.name + "/" + TARGETOBJNAME);
            if (anchorTargetObj == null)
            {
                anchorTargetObj = new GameObject(TARGETOBJNAME);
                anchorTargetObj.transform.parent = obj.transform;
            }
            anchorTargetObj.transform.position = targetPosTrans.position;

            // SkiinedMeshRendererに設定
            if (isGettingSkinnedMeshRenderer)
            {
                int index = 0;
                var skinnedMeshes = skinnedMeshRendererList;
                foreach (var skinnedMesh in skinnedMeshes)
                {
                    if (isSettingToSkinnedMesh.Length <= index) break;

                    if (isSettingToSkinnedMesh[index])
                        skinnedMesh.probeAnchor = anchorTargetObj.transform;
                    else
                        skinnedMesh.probeAnchor = null;

                    index++;
                }
            }

            // MeshRendererに設定
            if (isGettingMeshRenderer)
            {
                int index = 0;
                var meshes = meshRendererList;
                foreach (var mesh in meshes)
                {
                    if (isSettingToMesh[index++])
                        mesh.probeAnchor = anchorTargetObj.transform;
                    else
                        mesh.probeAnchor = null;
                }
            }
        }

    }

}
