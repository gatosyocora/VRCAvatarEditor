using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRCAvatarEditor
{
    // TODO: 含まれるかではなく前方、後方一致ではダメか？
    public enum ExclusionMatchType
    {
        Perfect, Contain
    }

    public class ExclusionBlendShape
    {
        public string Name { get; set; }
        public ExclusionMatchType MatchType { get; set; }

        public ExclusionBlendShape(string name, ExclusionMatchType type)
        {
            Name = name;
            MatchType = type;
        }
    }
}
