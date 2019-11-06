using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRCSDK2;
using UnityEditor;
using System;
using System.Linq;

namespace VRCAvatarEditor
{
    public class AvatarInfoGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatarEditor.Avatar avatar;

        private bool isOpeningLipSync = false;
        private Vector2 lipSyncScrollPos = Vector2.zero;
        private const int LIPSYNC_SHYPEKEY_NUM = 15;

        public AvatarInfoGUI(ref VRCAvatarEditor.Avatar avatar)
        {
            this.avatar = avatar;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            if (avatar.descriptor != null)
            {
                // 性別
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.sex = (VRC_AvatarDescriptor.AnimationSet)EditorGUILayout.EnumPopup("Gender", avatar.sex);

                    if (check.changed) avatar.descriptor.Animations = avatar.sex;
                }

                // アップロード状態
                EditorGUILayout.LabelField("Status", (string.IsNullOrEmpty(avatar.avatarId)) ? "New Avatar" : "Uploaded Avatar");
                avatar.animator.runtimeAnimatorController = EditorGUILayout.ObjectField(
                    "Animator",
                    avatar.animator.runtimeAnimatorController,
                    typeof(AnimatorOverrideController),
                    true
                ) as RuntimeAnimatorController;

                // AnimatorOverrideController
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.standingAnimController = EditorGUILayout.ObjectField(
                        "Standing Animations",
                        avatar.standingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;
                    avatar.sittingAnimController = EditorGUILayout.ObjectField(
                        "Sitting Animations",
                        avatar.sittingAnimController,
                        typeof(AnimatorOverrideController),
                        true
                    ) as AnimatorOverrideController;

                    if (check.changed)
                    {
                        avatar.descriptor.CustomStandingAnims = avatar.standingAnimController;
                        avatar.descriptor.CustomSittingAnims = avatar.sittingAnimController;
                    }
                }

                // ポリゴン数
                EditorGUILayout.LabelField("Triangles", avatar.triangleCount + "(" + (avatar.triangleCount + avatar.triangleCountInactive) + ")");

                // View Position
                using (new EditorGUILayout.HorizontalScope())
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        avatar.eyePos = EditorGUILayout.Vector3Field("View Position", avatar.eyePos);

                        if (check.changed)
                        {
                            avatar.descriptor.ViewPosition = avatar.eyePos;
                        }

                        if (GUILayout.Button("Auto Setting"))
                        {
                            avatar.eyePos = CalcAvatarViewPosition(avatar);
                            avatar.descriptor.ViewPosition = avatar.eyePos;
                        }
                    }
                }

                // リップシンク
                string lipSyncWarningMessage = "リップシンクが正しく設定されていない可能性があります";
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.lipSyncStyle = (VRC_AvatarDescriptor.LipSyncStyle)EditorGUILayout.EnumPopup("LipSync", avatar.lipSyncStyle);

                    if (check.changed) avatar.descriptor.lipSync = avatar.lipSyncStyle;
                }
                if (avatar.lipSyncStyle == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                {
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        avatar.faceMesh = EditorGUILayout.ObjectField(
                            "Face Mesh",
                            avatar.faceMesh,
                            typeof(SkinnedMeshRenderer),
                            true
                        ) as SkinnedMeshRenderer;

                        if (check.changed)
                            avatar.descriptor.VisemeSkinnedMesh = avatar.faceMesh;
                    }
                    if (avatar.faceMesh != null)
                    {
                        isOpeningLipSync = EditorGUILayout.Foldout(isOpeningLipSync, "ShapeKeys");
                        if (isOpeningLipSync)
                        {
                            using (new EditorGUI.IndentLevelScope())
                            using (var scrollView = new EditorGUILayout.ScrollViewScope(lipSyncScrollPos))
                            {
                                lipSyncScrollPos = scrollView.scrollPosition;

                                for (int visemeIndex = 0; visemeIndex < LIPSYNC_SHYPEKEY_NUM; visemeIndex++)
                                {
                                    EditorGUILayout.LabelField("Viseme:" + Enum.GetName(typeof(VRC_AvatarDescriptor.Viseme), visemeIndex), avatar.descriptor.VisemeBlendShapes[visemeIndex]);
                                }
                            }
                        }
                    }
                }
                if (avatar.lipSyncStyle != VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape || avatar.faceMesh == null)
                {
                    EditorGUILayout.HelpBox(lipSyncWarningMessage, MessageType.Warning);
                    if (GUILayout.Button("シェイプキーによるリップシンクを自動設定する"))
                    {
                        avatar.SetLipSyncToViseme();
                    }
                }

                EditorGUILayout.Space();
            }
            return false;
        }

        public void DrawSettingsGUI() { }
        public void LoadSettingData(SettingData settingAsset) { }
        public void SaveSettingData(ref SettingData settingAsset) { }
        public void Dispose() { }

        private Vector3 CalcAvatarViewPosition(VRCAvatarEditor.Avatar avatar)
        {
            var viewPos = Vector3.zero;
            var animator = avatar.animator;
            var objTrans = avatar.descriptor.transform;

            // leftEyeとRightEyeの位置からx, yを計算する
            var leftEyeTrans = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var rightEyeTrans = animator.GetBoneTransform(HumanBodyBones.RightEye);
            viewPos = (leftEyeTrans.position + rightEyeTrans.position) / 2f;


            var renderer = avatar.faceMesh;
            var mesh = renderer.sharedMesh;
            var vertices = mesh.vertices;
            var local2WorldMat = renderer.transform.localToWorldMatrix;

            // 左目メッシュの頂点のうち, 左目ボーンから一番離れた頂点までの距離を計算する
            var leftEyeBoneIndex = Array.IndexOf(renderer.bones, leftEyeTrans);
            var leftEyeMeshVertexIndices = mesh.boneWeights
                                .Select((x, index) => new { index = index, boneIndex0 = x.boneIndex0, boneIndex1 = x.boneIndex1, boneIndex2 = x.boneIndex2, boneIndex3 = x.boneIndex3})
                                .Where(x => x.boneIndex0 == leftEyeBoneIndex || x.boneIndex1 == leftEyeBoneIndex || x.boneIndex2 == leftEyeBoneIndex || x.boneIndex3 == leftEyeBoneIndex)
                                .Select(x => x.index)
                                .ToArray();
            var leftMaxDistance = 0f;
            foreach (var index in leftEyeMeshVertexIndices)
                if (leftMaxDistance < Vector3.Distance(local2WorldMat.MultiplyPoint3x4(vertices[index]), leftEyeTrans.position))
                    leftMaxDistance = Vector3.Distance(local2WorldMat.MultiplyPoint3x4(vertices[index]), leftEyeTrans.position);


            // 右目も同じく計算する
            var rightEyeBoneIndex = Array.IndexOf(renderer.bones, rightEyeTrans);
            var rightEyeMeshVertexIndices = mesh.boneWeights
                                .Select((x, index) => new { index = index, boneIndex0 = x.boneIndex0, boneIndex1 = x.boneIndex1, boneIndex2 = x.boneIndex2, boneIndex3 = x.boneIndex3 })
                                .Where(x => x.boneIndex0 == rightEyeBoneIndex || x.boneIndex1 == rightEyeBoneIndex || x.boneIndex2 == rightEyeBoneIndex || x.boneIndex3 == rightEyeBoneIndex)
                                .Select(x => x.index)
                                .ToArray();
            var rightMaxDistance = 0f;
            foreach (var index in rightEyeMeshVertexIndices)
                if (rightMaxDistance < Vector3.Distance(local2WorldMat.MultiplyPoint3x4(vertices[index]), rightEyeTrans.position))
                    rightMaxDistance = Vector3.Distance(local2WorldMat.MultiplyPoint3x4(vertices[index]), rightEyeTrans.position);

            viewPos.z += (leftMaxDistance + rightMaxDistance) / 2f;

            // ローカル座標に変換
            viewPos = objTrans.worldToLocalMatrix.MultiplyPoint3x4(viewPos);

            return viewPos;
        }
    }
}

