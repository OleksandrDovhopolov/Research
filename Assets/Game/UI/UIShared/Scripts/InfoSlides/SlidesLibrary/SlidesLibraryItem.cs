using System;
using UnityEngine;

namespace UIShared
{
    [CreateAssetMenu(fileName = "SlidesLibraryItem", menuName = "InfoSlides/SlidesLibraryItem")]
    public class SlidesLibraryItem : ScriptableObject
    {
        public SlidesType SlidesType => _slidesType; 
        [SerializeField] private SlidesType _slidesType;
        [SerializeField] private int _maxTitleLineLength = 16;
        public int MaxTitleLineLength => _maxTitleLineLength;
        
        [field: SerializeField] public string TitleKey { get; protected set; }
        [field: SerializeField] public string DescriptionKey { get; protected set; }

        [SerializeField] private SlideInfo[] _slidesInfos;

        public SlideInfo this[int index] => _slidesInfos[index];

        [Serializable]
        public class SlideInfo
        {
            [field: SerializeField] public GameObject Slide { get; private set; }
            [field: SerializeField] public string SlideKey { get; private set; }
        }
    }
}