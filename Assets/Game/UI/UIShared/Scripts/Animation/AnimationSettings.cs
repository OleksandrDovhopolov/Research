using System;
using System.Collections.Generic;
using CoreResources;
using UnityEngine;

namespace UIShared
{
    [CreateAssetMenu(fileName = "AnimationSettings", menuName = "AnimationSettings")]
    public class AnimationSettings : ScriptableObject
    {
        [SerializeField]
        private List<ParticleAnimationParameters> _animationParameters;

        public ParticleAnimationParameters GetResourceAnimationParameters(ResourceType resourceType)
        {
            var animationSettings = _animationParameters.Find(parameters => parameters.ResourceType == resourceType);
            if (animationSettings != null)
            {
                return animationSettings;
            }
            
            throw new Exception("AnimationSettings not found with resource type: " + resourceType);
        }
    }
    
    [Serializable]
    public class ParticleAnimationParameters
    {
        public AnimationCurve ParticleAccelerationCurve;
        public ResourceType ResourceType;
        public Sprite Sprite;
        
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