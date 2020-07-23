using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(menuName = "VRCAvatarEditor/LanguageData")]
public class LanguageKeyPair : ScriptableObject
{
    [Header("General")]
    public string avatar;
    public string reset;
    public string close;
    public string edit;
    public string selectFolder;
    public string exclusions;
    public string select;

    [Header("VRCAvatarEditorGUI")]
    public string reloadAvatarButtonText;
    public string toolInfoButtonText;
    public string settingButtonText;
    public string resetPoseButtonText;
    public string uploadAvatarButtonText;

    [Header("Tab")]
    public string avatarInfoTitle;
    public string faceEmotionTitle;
    public string probeAnchorTitle;
    public string boundsTitle;
    public string shaderTitle;

    [Header("AnimationsGUI")]
    public string standingTabText;
    public string sittingTabText;
    public string emoteButtonText;
    public string faceAndHandButtonText;
    public string standingTitle;
    public string sittingTitle;
    public string noAvatarMessage;

    [Header("AvatarInfoGUI")]
    public string genderLabel;
    public string uploadStatusLabel;
    public string newAvatarText;
    public string uploadedAvatarText;
    public string customStandingAnimsLabel;
    public string customSittingAnimsLabel;
    public string triangleCountLabel;
    public string faceMeshLabel;
    public string viewPositionLabel;
    public string lipSyncTypeLabel;
    public string lipSyncBlendShapesLabel;

    [Header("FaceEmotionGUI")]
    public string loadAnimationButtonText;
    public string setToDefaultButtonText;
    public string resetToDefaultButtonText;
    public string toggleAllLabel;
    public string minButtonText;
    public string maxButtonText;
    public string animClipFileNameLabel;
    public string animClipSaveFolderLabel;
    public string animationOverrideLabel;
    public string handPoseAnimClipLabel;
    public string createAnimFileButtonText;

    [Header("ProbeAnchorGUI")]
    public string setToSkinnedMeshRendererLabel;
    public string setToMeshRendererLabel;
    public string targetPositionLabel;
    public string rendererListLabel;
    public string setProbeAnchorButtonText;

    [Header("BoundsGUI")]
    public string resetToBoundsToPrefabButtonText;
    public string childObjectsLabel;
    public string setBoundsButtonText;

    [Header("AppInfoGUI")]
    public string versionLabel;
    public string openOnlineManualButtonText;
    public string functionsLabel;
    public string readmeLabel;
    public string licenseLabel;
    public string usingSoftwareLicenseLabel;

    [Header("SettingGUI")]
    public string settingPageMessageText;
    public string saveSettingButtonText;
    public string changeDefaultSettingButtonText;
    // AvatarMonitor
    public string defaultCameraDistanceLabel;
    public string faceCameraDistanceLabel;
    public string cameraZoomStepDistanceLabel;
    public string gammaCorrectionLabel;
    public string monitorBackgroundColorLabel;
    public string monitorSizeTypeLabel;
    public string monitorSizeLabel;
    // FaceEmotionCreator
    public string sortTypeLabel;
    public string blendShapeExclusionsLabel;
    public string usePreviousAnimationOnHandAnimationLabel;
    // Other
    public string layoutTypeLabel;
}
