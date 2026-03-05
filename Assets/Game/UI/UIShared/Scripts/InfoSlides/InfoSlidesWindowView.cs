using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using UISystem;
using UnityEngine;
using UnityEngine.UI;

namespace UIShared
{
    public class InfoSlidesWindowView : WindowView
    {
        [SerializeField] private RectTransform _titleRoot;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private InfoSlidesWindowItem[] _slides;
        [SerializeField] private InfoSlidesLibrary _library;

        private readonly List<GameObject> _resources = new();
        
        public void UpdateWindow(SlidesType type,Func<GameObject, Transform, GameObject> instantiator)
        {
            var slidesLibraryItem = _library.Get(type);

            _title.text = SplitIntoLines(
                slidesLibraryItem.TitleKey.Trim(), slidesLibraryItem.MaxTitleLineLength);
            
            for (var i = 0; i < _slides.Length; i++)
            {
                _slides[i].Text.text = slidesLibraryItem[i].SlideKey;
                var slide = instantiator(slidesLibraryItem[i].Slide, _slides[i].Placeholder);
                _resources.Add(slide);
            }
            
            ResizeLayout();
        }

        private async void ResizeLayout()
        {
            await UniTask.Yield();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_titleRoot);
        }
        
        private void OnDisable()
        {
            foreach (var resource in _resources)
                Destroy(resource);
            
            _resources.Clear();
        }
        
        public static string SplitIntoLines(string source, int lineLength)
        {
            var formattedText = new StringBuilder();
            var start = 0;
            
            while (start < source.Length)
            {
                var end = start + lineLength;
                if (end >= source.Length)
                {
                    formattedText.Append(source[start..]);
                    break;
                }

                var spaceIndex = FindClosestSpace(source, end);

                if (spaceIndex == -1)
                {
                    formattedText.Append(source[start..]);
                    break;
                }
                formattedText.Append(source.Substring(start, spaceIndex - start) + "\n");
                start = spaceIndex + 1;
            }

            return formattedText.ToString();
        }
        
        public static int FindClosestSpace(string source, int index)
        {
            if (char.IsWhiteSpace(source[index])) return index;
            var counter = 0;
            do
            {
                counter++;
                if (index - counter >= 0 && char.IsWhiteSpace(source[index - counter]))
                    return index - counter;
                if (index + counter < source.Length && char.IsWhiteSpace(source[index + counter]))
                    return index + counter;
            } while (index - counter >= 0 || index + counter < source.Length);
            
            return -1;
        }
    }
}