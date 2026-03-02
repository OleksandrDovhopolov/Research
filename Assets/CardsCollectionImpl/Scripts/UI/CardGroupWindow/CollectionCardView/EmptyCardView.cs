using UnityEngine;

namespace CardCollectionImpl
{
    public class EmptyCardView : MonoBehaviour
    {
       [SerializeField] private CanvasGroup _canvasGroup;

       public CanvasGroup CanvasGroup => _canvasGroup;
    }
}