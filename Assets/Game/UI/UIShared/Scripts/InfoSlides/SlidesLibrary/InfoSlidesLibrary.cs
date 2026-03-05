
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UIShared
{
    [CreateAssetMenu(fileName = "InfoSlidesLibrary", menuName = "InfoSlides/InfoSlidesLibrary")]
    public class InfoSlidesLibrary : ScriptableObject
    {
        [SerializeField] private List<SlidesLibraryItem> _slides;

        private Dictionary<SlidesType, SlidesLibraryItem> _slidesByType;
    
        public SlidesLibraryItem Get(SlidesType type)
        {
            _slidesByType ??= _slides.ToDictionary(i => i.SlidesType);
            return _slidesByType[type];
        }
    }
}