using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Copyright (c) 2019 gatosyocora

namespace VRCAvatarEditor
{
    public class SettingData : ScriptableObject
    {
        public float defaultZoomDist = 1.0f;
        public float faceZoomDist = 0.5f;
        public float zoomStepDist = 0.25f;

        public bool isGammaCorrection = true;
        public Color monitorBgColor = new Color(0.95f, 0.95f, 0.95f, 1);

        public bool isActiveOnlySelectedAvatar = true;

        public VRCAvatarEditor.SortType selectedSortType = VRCAvatarEditor.SortType.UnSort;
        public List<string> blendshapeExclusions = new List<string> { "vrc.v_", "vrc.blink_", "vrc.lowerlid_", "vrc.owerlid_", "mmd" };

        public VRCAvatarEditor.LayoutType layoutType = VRCAvatarEditor.LayoutType.Default;
    }
}


