using UnityEngine;

namespace core
{
    public class AnimationCardOpener : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private NewCardView _newCardView;

        public void PlayCardOpen()
        {
            Debug.LogWarning($"Debug Test PlayCardOpen");
            _animator.SetBool("isOpen", true);
        }

        public void PlayCardClosed()
        {
            Debug.LogWarning($"Debug Test PlayCardClosed");
            _animator.SetBool("isOpen", false);
        }

        public void AnimateCards()
        {
            _newCardView.CreateMocks();
        }
    }
}