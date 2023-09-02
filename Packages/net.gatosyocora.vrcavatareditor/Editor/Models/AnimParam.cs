using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRCAvatarEditor
{
    public class AnimParam
    {
        public string ObjPath { set; get; }
        public string BlendShapeName { set; get; }
        public float Value { set; get; }
        public bool IsSelect { set; get; }

        public AnimParam(string path, string propertyName, float value)
        {
            ObjPath = path;
            this.BlendShapeName = propertyName.Replace("blendShape.", "");
            this.Value = value;
            IsSelect = true;
        }
    }
}
