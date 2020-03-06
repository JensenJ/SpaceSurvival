using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUCL.UI
{
    [CreateAssetMenu(menuName = "JUCL/UI/UIAnimationAsset")]
    public class JUCLUIAnimationAsset : ScriptableObject
    {

        //Settings for the enabling animation
        [Header("Enable Settings")]

        public UIAnimationTypes inAnimationType = UIAnimationTypes.Fade;
        public LeanTweenType inEaseType = LeanTweenType.notUsed;

        public float inDuration = 0.5f;
        public float inDelay = 0.0f;

        public bool inLoop = false;
        public bool inPingpong = false;
        public bool inStartPositionOffset = true;

        public Vector3 inFrom = new Vector3();
        public Vector3 inTo = new Vector3();
        public bool showOnEnable = true;

        [Header("Disable Settings")]
        public UIAnimationTypes outAnimationType = UIAnimationTypes.Fade;
        public LeanTweenType outEaseType = LeanTweenType.notUsed;

        public float outDuration = 0.5f;
        public float outDelay = 0.0f;

        public bool outLoop = false;
        public bool outPingpong = false;
        public bool outStartPositionOffset = true;

        public Vector3 outFrom = new Vector3();
        public Vector3 outTo = new Vector3();
        public bool destroyOnDisable = false;
    }
}