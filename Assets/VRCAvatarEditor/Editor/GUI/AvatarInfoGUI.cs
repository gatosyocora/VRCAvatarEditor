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

        public void Initialize(ref VRCAvatarEditor.Avatar avatar)
        {
            this.avatar = avatar;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            if (avatar != null && avatar.descriptor != null)
            {
                // 性別
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.sex = (VRC_AvatarDescriptor.AnimationSet)EditorGUILayout.EnumPopup("Gender", avatar.sex);

                    if (check.changed)
                    {
                        avatar.descriptor.Animations = avatar.sex;
                        EditorUtility.SetDirty(avatar.descriptor);
                    }
                }

                // アップロード状態
                EditorGUILayout.LabelField("Status", (string.IsNullOrEmpty(avatar.avatarId)) ? "New Avatar" : "Uploaded Avatar");

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
                        EditorUtility.SetDirty(avatar.descriptor);

                        avatar.SetAnimSavedFolderPath();
                    }
                }

                // ポリゴン数
                EditorGUILayout.LabelField("Triangles", avatar.triangleCount + "(" + (avatar.triangleCount + avatar.triangleCountInactive) + ")");

                // faceMesh
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.faceMesh = EditorGUILayout.ObjectField(
                        "Face Mesh",
                        avatar.faceMesh,
                        typeof(SkinnedMeshRenderer),
                        true
                    ) as SkinnedMeshRenderer;

                    if (check.changed)
                    {
                        avatar.descriptor.VisemeSkinnedMesh = avatar.faceMesh;
                        EditorUtility.SetDirty(avatar.descriptor);
                    }
                }

                EditorGUILayout.Space();

                // View Position
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.eyePos = EditorGUILayout.Vector3Field("View Position", avatar.eyePos);

                    if (check.changed)
                    {
                        avatar.descriptor.ViewPosition = avatar.eyePos;
                        EditorUtility.SetDirty(avatar.descriptor);
                    }
                }
                /*
                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledGroupScope(avatar.faceMesh == null))
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Auto Setting"))
                    {
                        avatar.eyePos = CalcAvatarViewPosition(avatar);
                        avatar.descriptor.ViewPosition = avatar.eyePos;
                    }

                    if (GUILayout.Button("Revert to Prefab"))
                    {
                        avatar.eyePos = RevertEyePosToPrefab(avatar.descriptor);
                    }
                }
                if (avatar.faceMesh == null)
                {
                    EditorGUILayout.HelpBox("ViewPositionを自動設定するためにはFaceMeshを設定する必要があります", MessageType.Warning);
                }
                */

                EditorGUILayout.Space();

                // リップシンク
                string lipSyncWarningMessage = "リップシンクが正しく設定されていない可能性があります";
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    avatar.lipSyncStyle = (VRC_AvatarDescriptor.LipSyncStyle)EditorGUILayout.EnumPopup("LipSync", avatar.lipSyncStyle);

                    if (check.changed) avatar.descriptor.lipSync = avatar.lipSyncStyle;
                }
                if (avatar.lipSyncStyle == VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape)
                {
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
                        EditorUtility.SetDirty(avatar.descriptor);
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

        // TODO : モデルによっては前髪あたりまでviewpositionがいってしまう
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
            // 右目も同様に計算し, その平均距離+eyeBoneの平均z位置をzとする
            var leftMaxDistance = CalcDistanceFromMaxFarVertexTo(leftEyeTrans, renderer);
            var rightMaxDistance = CalcDistanceFromMaxFarVertexTo(rightEyeTrans, renderer);
            viewPos.z += (leftMaxDistance + rightMaxDistance) / 2f;

            var leftEyeBoneIndex = Array.IndexOf(renderer.bones, leftEyeTrans);
            var boneWeights = renderer.sharedMesh.boneWeights
                                .Where(x => x.boneIndex0 == leftEyeBoneIndex ||
                                            x.boneIndex1 == leftEyeBoneIndex ||
                                            x.boneIndex2 == leftEyeBoneIndex ||
                                            x.boneIndex3 == leftEyeBoneIndex )
                                .ToArray();

            // ローカル座標に変換
            viewPos = objTrans.worldToLocalMatrix.MultiplyPoint3x4(viewPos);

            return viewPos;
        }

        private float CalcDistanceFromMaxFarVertexTo(Transform targetBone, SkinnedMeshRenderer renderer)
        {
            var targetBoneIndex = Array.IndexOf(renderer.bones, targetBone);
            var meshVertexIndices = renderer.sharedMesh.boneWeights
                                .Select((x, index) => new { index = index, value = x })
                                .Where(x => (x.value.boneIndex0 == targetBoneIndex && x.value.weight0 > 0f) ||
                                            (x.value.boneIndex1 == targetBoneIndex && x.value.weight1 > 0f) ||
                                            (x.value.boneIndex2 == targetBoneIndex && x.value.weight2 > 0f) ||
                                            (x.value.boneIndex3 == targetBoneIndex && x.value.weight3 > 0f))
                                .Select(x => x.index)
                                .ToArray();
            var maxDistance = 0f;
            var vertices = renderer.sharedMesh.vertices;
            var local2WorldMatrix = renderer.transform.localToWorldMatrix;
            foreach (var index in meshVertexIndices)
                if (maxDistance < Vector3.Distance(local2WorldMatrix.MultiplyPoint3x4(vertices[index]), targetBone.position))
                    maxDistance = Vector3.Distance(local2WorldMatrix.MultiplyPoint3x4(vertices[index]), targetBone.position);

            return maxDistance;
        }

        private static Vector3 RevertEyePosToPrefab(VRC_AvatarDescriptor descriptor)
        {
            PrefabUtility.ReconnectToLastPrefab(descriptor.gameObject);

            var so = new SerializedObject(descriptor);
            so.Update();

            var sp = so.FindProperty("ViewPosition");
#if UNITY_2018_3_OR_NEWER
            // Transform has 'ReflectionProbeAnchorManager::kChangeSystem' change interests present when destroying the hierarchy.
            // 対策で一度disableにする
            descriptor.enabled = false;
            PrefabUtility.RevertPropertyOverride(sp, InteractionMode.UserAction);
            descriptor.enabled = true;
#else
            sp.prefabOverride = false;
            sp.serializedObject.ApplyModifiedProperties();
#endif
            return descriptor.ViewPosition;
        }
    }
}

