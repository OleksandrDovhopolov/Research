using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIShared
{
    [CreateAssetMenu(fileName = "AnimationSettings", menuName = "AnimationSettings")]
    public class AnimationSettings : ScriptableObject
    {
        private const string DefaultAnimationParams = "default";
        
        [SerializeField]
        private List<ParticleAnimationParameters> _animationParameters;

        public ParticleAnimationParameters GetResourceAnimationParameters(string itemType)
        {
            var animationSettings = _animationParameters.Find(parameters => parameters.ItemType == itemType);
            if (animationSettings != null)
            {
                return animationSettings;
            }
            
            return _animationParameters.Find(parameters => parameters.ItemType == DefaultAnimationParams);
        }
    }
    
    [Serializable]
    public class ParticleAnimationParameters
    {
        public AnimationCurve ParticleAccelerationCurve;
        public string ItemType;
        
        public Vector3 StartScaleValue = Vector3.one;
        public float UpScaleDuration = 0.5f;
        public float DownScaleDuration = 0.3f;
        public float UpScaleValue = 1.5f;
        public float DownScaleValue = 0.5f;
        public float UpScaleStartTime = 0.1f;
        public float DownScaleStartTime = 0.7f;
        public float CurveAnimTime = 1f;
        public int CurveRange = 600;
        public int CurveReturnDistance = 300;
        public float CurveAngle = 1;
        public AnimationCurve ScaleEase;
        public AnimationCurve FlightEase;
        public float SpawnDelay = 0.1f;
        public float OverwhelmMin = -0.3f;
        public float OverwhelmMax = -0.5f;


        public ParticleAnimationParameters Clone()
        {
            return (ParticleAnimationParameters) MemberwiseClone();
        }
    }
}