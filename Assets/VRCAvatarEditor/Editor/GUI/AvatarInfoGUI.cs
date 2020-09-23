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
        private VRCAvatar originalAvatar;
        private VRCAvatar editAvatar;

        private AvatarMonitorGUI monitorGUI;

        private bool isOpeningLipSync = false;
        private Vector2 lipSyncScrollPos = Vector2.zero;
        private const int LIPSYNC_SHYPEKEY_NUM = 15;

        private Texture2D showIconTexture;
        private Texture2D hideIconTexture;

        public void Initialize(VRCAvatar originalAvatar, VRCAvatar editAvatar, AvatarMonitorGUI monitorGUI)
        {
            this.originalAvatar = originalAvatar;
            this.editAvatar = editAvatar;
            this.monitorGUI = monitorGUI;

            showIconTexture = Resources.Load<Texture2D>("Icon/ShowIcon");
            hideIconTexture = Resources.Load<Texture2D>("Icon/HideIcon");
        }

        public bool DrawGUI(GUILayoutOption[] layoutOptions)
        {
            EditorGUILayout.LabelField(LocalizeText.instance.langPair.avatarInfoTitle, EditorStyles.boldLabel);

            if (originalAvatar != null && originalAvatar.Descriptor != null)
            {
                using (new EditorGUILayout.VerticalScope(GUI.skin.box))
                {
                    // 性別
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        originalAvatar.Sex = (AnimationSet)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.genderLabel, originalAvatar.Sex);

                        if (check.changed)
                        {
                            originalAvatar.Descriptor.Animations = originalAvatar.Sex;
                            EditorUtility.SetDirty(originalAvatar.Descriptor);
                        }
                    }

                    // アップロード状態
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.uploadStatusLabel,
                        (string.IsNullOrEmpty(originalAvatar.AvatarId)) ?
                            LocalizeText.instance.langPair.newAvatarText :
                            LocalizeText.instance.langPair.uploadedAvatarText);

                    // AnimatorOverrideController
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
#if VRC_SDK_VRCSDK2
                        originalAvatar.StandingAnimController = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.customStandingAnimsLabel,
                            originalAvatar.StandingAnimController);

                        originalAvatar.SittingAnimController = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.customSittingAnimsLabel,
                            originalAvatar.SittingAnimController);
#elif VRC_SDK_VRCSDK3
                        originalAvatar.GestureController = GatoGUILayout.ObjectField(
                            "Gesture Layer",
                            originalAvatar.GestureController);

                        originalAvatar.FxController = GatoGUILayout.ObjectField(
                            "FX Layer",
                            originalAvatar.FxController);
#endif

                        if (check.changed)
                        {
#if VRC_SDK_VRCSDK2
                            originalAvatar.Descriptor.CustomStandingAnims = originalAvatar.StandingAnimController;
                            originalAvatar.Descriptor.CustomSittingAnims = originalAvatar.SittingAnimController;
#elif VRC_SDK_VRCSDK3
                            originalAvatar.Descriptor.baseAnimationLayers[2].animatorController = originalAvatar.GestureController;
                            originalAvatar.Descriptor.baseAnimationLayers[4].animatorController = originalAvatar.FxController;
#endif
                            EditorUtility.SetDirty(originalAvatar.Descriptor);

                            originalAvatar.SetAnimSavedFolderPath();
                        }
                    }

                    // ポリゴン数
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.triangleCountLabel, originalAvatar.TriangleCount + "(" + (originalAvatar.TriangleCount + originalAvatar.TriangleCountInactive) + ")");

                    // 身長
                    EditorGUILayout.LabelField(LocalizeText.instance.langPair.heightLabel, $"{originalAvatar.Height:F2} m");

                    // View Position
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(LocalizeText.instance.langPair.viewPositionLabel, GUILayout.Width(145f));
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            originalAvatar.EyePos = EditorGUILayout.Vector3Field(string.Empty, originalAvatar.EyePos);

                            if (check.changed)
                            {
                                originalAvatar.Descriptor.ViewPosition = originalAvatar.EyePos;
                                editAvatar.Descriptor.ViewPosition = originalAvatar.EyePos;
                                editAvatar.EyePos = originalAvatar.EyePos;
                                EditorUtility.SetDirty(originalAvatar.Descriptor);

                                monitorGUI.showEyePosition = true;
                            }
                        }

                        monitorGUI.showEyePosition = GatoGUILayout.ToggleImage(
                                                        monitorGUI.showEyePosition,
                                                        showIconTexture,
                                                        hideIconTexture);
                    }

                    // faceMesh
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        originalAvatar.FaceMesh = GatoGUILayout.ObjectField(
                            LocalizeText.instance.langPair.faceMeshLabel,
                            originalAvatar.FaceMesh);

                        if (check.changed)
                        {
                            originalAvatar.Descriptor.VisemeSkinnedMesh = originalAvatar.FaceMesh;
                            EditorUtility.SetDirty(originalAvatar.Descriptor);
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
                        originalAvatar.LipSyncStyle = (LipSyncStyle)EditorGUILayout.EnumPopup(LocalizeText.instance.langPair.lipSyncTypeLabel, originalAvatar.LipSyncStyle);

                        if (check.changed) originalAvatar.Descriptor.lipSync = originalAvatar.LipSyncStyle;
                    }
                    if (originalAvatar.LipSyncStyle == LipSyncStyle.VisemeBlendShape)
                    {
                        if (originalAvatar.FaceMesh != null)
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
                                        EditorGUILayout.LabelField("Viseme:" + Enum.GetName(typeof(Viseme), visemeIndex), originalAvatar.Descriptor.VisemeBlendShapes[visemeIndex]);
                                    }
                                }
                            }
                        }
                    }
                    if (originalAvatar.LipSyncStyle != LipSyncStyle.VisemeBlendShape || originalAvatar.FaceMesh == null)
                    {
                        EditorGUILayout.HelpBox(LocalizeText.instance.langPair.lipSyncWarningMessageText, MessageType.Warning);
                        GatoGUILayout.Button(
                            LocalizeText.instance.langPair.lipSyncBlendShapesAutoDetectButtonText,
                            () => {
                                originalAvatar.SetLipSyncToViseme();
                                EditorUtility.SetDirty(originalAvatar.Descriptor);
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

