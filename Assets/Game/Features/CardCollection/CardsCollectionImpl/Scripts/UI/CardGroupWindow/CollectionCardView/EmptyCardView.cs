using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CardCollectionImpl
{
    public class EmptyCardView : MonoBehaviour
    {
       [SerializeField] private CanvasGroup _canvasGroup;

       [Space, Header("Frame")]
       [SerializeField] private List<Image> _cardFrameImage;
       [SerializeField] private Color _defaultFrameColor;
       [SerializeField] private Color _premiumFrameColor;
       
       public CanvasGroup CanvasGroup => _canvasGroup;
       
       public void UpdateCardFrame(bool premiumCard)
       {
           var frameColor = premiumCard ? _premiumFrameColor : _defaultFrameColor;
           foreach (var image in _cardFrameImage)
           {
               image.color = frameColor;
           }
       }
    }
}