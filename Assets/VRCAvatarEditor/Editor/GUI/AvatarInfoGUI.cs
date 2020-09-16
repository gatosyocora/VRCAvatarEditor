using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if VRC_SDK_VRCSDK2
using VRCSDK2;
using VRCAvatar = VRCAvatarEditor.Avatars2.VRCAvatar2;
#elif VRC_SDK_VRCSDK3
using VRCAvatar = VRCAvatarEditor.Avatars3.VRCAvatar3;
#endif
using LipSyncStyle = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle;
using AnimationSet = VRC.SDKBase.VRC_AvatarDescriptor.AnimationSet;
using Viseme = VRC.SDKBase.VRC_AvatarDescriptor.Viseme;

namespace VRCAvatarEditor
{
    public class AvatarInfoGUI : Editor, IVRCAvatarEditorGUI
    {
        private VRCAvatar avatar;

        private bool isOpeningLipSync = false;
        private Vector2 lipSyncScrollPos = Vector2.zero;
        private const int LIPSYNC_SHYPEKEY_NUM = 15;

        public void Initialize(VRCAvatar avatar)
        {
            this.avatar = avatar;
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.avatarInfoTitle, EditorStyles.boldLabel);

            if (avatar != null && avatar.Descriptor != null)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    // 性別
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        avatar.Sex = (AnimationSet)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.genderLabel, avatar.Sex);

                        if (check.changed)
                        {
                            avatar.Descriptor.Animations = avatar.Sex;
                            EditorUtility.SetDirty(avatar.Descriptor);
                        }
                    }

                    // アップロード状態
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.uploadStatusLabel,
                        (string.IsNullOrEmpty(avatar.AvatarId)) ?
                            LocalizeText.instance.langPair.newAvatarText :
                            LocalizeText.instance.langPair.uploadedAvatarText);

                    // AnimatorOverrideController
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
#if VRC_SDK_VRCSDK2
                        avatar.StandingAnimController = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.customStandingAnimsLabel,
                            avatar.StandingAnimController);

                        avatar.SittingAnimController = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.customSittingAnimsLabel,
                            avatar.SittingAnimController);
#elif VRC_SDK_VRCSDK3
                        avatar.GestureController = GatoGUILayout.ObjectField(
                            "Gesture Layer",
                            avatar.GestureController);

                        avatar.FxController = GatoGUILayout.ObjectField(
                            "FX Layer",
                            avatar.FxController);
#endif

                        if (check.changed)
                        {
#if VRC_SDK_VRCSDK2
                            avatar.Descriptor.CustomStandingAnims = avatar.StandingAnimController;
                            avatar.Descriptor.CustomSittingAnims = avatar.SittingAnimController;
#elif VRC_SDK_VRCSDK3
                            avatar.Descriptor.baseAnimationLayers[2].animatorController = avatar.GestureController;
                            avatar.Descriptor.baseAnimationLayers[4].animatorController = avatar.FxController;
#endif
                            EditorUtility.SetDirty(avatar.Descriptor);

                            avatar.SetAnimSavedFolderPath();
                        }
                    }

                    // ポリゴン数
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.triangleCountLabel, avatar.TriangleCount + "(" + (avatar.TriangleCount + avatar.TriangleCountInactive) + ")");

                    // 身長
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.heightLabel, $"{avatar.Height:F2} m");

                    // faceMesh
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        avatar.FaceMesh = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.faceMeshLabel,
                            avatar.FaceMesh);

                        if (check.changed)
                        {
                            avatar.Descriptor.VisemeSkinnedMesh = avatar.FaceMesh;
                            EditorUtility.SetDirty(avatar.Descriptor);
                        }
                    }

                    EditorGUILayout.Space();

                    // View Position
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        avatar.EyePos = EditorGUILayout.Vector3Field(LocalizeText.instance.langPair.viewPositionLabel, avatar.EyePos);

                        if (check.changed)
                        {
                            avatar.Descriptor.ViewPosition = avatar.EyePos;
                            EditorUtility.SetDirty(avatar.Descriptor);
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
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        avatar.LipSyncStyle = (LipSyncStyle)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.lipSyncTypeLabel, avatar.LipSyncStyle);

                        if (check.changed) avatar.Descriptor.lipSync = avatar.LipSyncStyle;
                    }
                    if (avatar.LipSyncStyle == LipSyncStyle.VisemeBlendShape)
                    {
                        if (avatar.FaceMesh != null)
                        {
                            isOpeningLipSync = EditorGUILayout.Foldout(isOpeningLipSync, LocalizeText.instance.langPair.lipSyncBlendShapesLabel);
                            if (isOpeningLipSync)
                            {
                                using (new EditorGUI.IndentLevelScope())
                                using (var scrollView = new EditorGUILayout.ScrollViewScope(lipSyncScrollPos))
                                {
                                    lipSyncScrollPos = scrollView.scrollPosition;

                                    for (int visemeIndex = 0; visemeIndex < LIPSYNC_SHYPEKEY_NUM; visemeIndex++)
                                    {
                                        EditorGUILayout.LabelField("Viseme:" + Enum.GetName(typeof(Viseme), visemeIndex), avatar.Descriptor.VisemeBlendShapes[visemeIndex]);
                                    }
                                }
                            }
                        }
                    }
                    if (avatar.LipSyncStyle != LipSyncStyle.VisemeBlendShape || avatar.FaceMesh == null)
                    {
                        EditorGUILayout.HelpBox(LocalizeText.instance.langPair.lipSyncWarningMessageText, MessageType.Warning);
                        GatoGUILayout.Button(
                            LocalizeText.instance.langPair.lipSyncBlendShapesAutoDetectButtonText,
                            () => {
                                avatar.SetLipSyncToViseme();
                                EditorUtility.SetDirty(avatar.Descriptor);
                            });
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
        private Vector3 CalcAvatarViewPosition(VRCAvatar avatar)
        {
            var viewPos = Vector3.zero;
            var animator = avatar.Animator;
            var objTrans = avatar.Animator.transform;

            // leftEyeとRightEyeの位置からx, yを計算する
            var leftEyeTrans = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            var rightEyeTrans = animator.GetBoneTransform(HumanBodyBones.RightEye);
            viewPos = (leftEyeTrans.position + rightEyeTrans.position) / 2f;

            var renderer = avatar.FaceMesh;
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
                                            x.boneIndex3 == leftEyeBoneIndex)
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

#if VRC_SDK_VRCSDK2
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
#endif
    }
}

