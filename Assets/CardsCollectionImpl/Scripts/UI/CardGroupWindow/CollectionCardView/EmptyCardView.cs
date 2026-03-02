using UnityEngine;

namespace core
{
    public class EmptyCardView : MonoBehaviour
    {
       [SerializeField] private CanvasGroup _canvasGroup;

       public CanvasGroup CanvasGroup => _canvasGroup;
    }
}