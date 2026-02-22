using UnityEngine;

namespace core
{
    public class AnimationCardOpener : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private NewCardView _newCardView;

        public void PlayCardOpen()
        {
            _animator.SetBool("isOpen", true);
        }

        public void PlayCardClosed()
        {
            _animator.SetBool("isOpen", false);
        }

        public void AnimateCards()
        {
            _newCardView.PlayOpenSequenceAsync(destroyCancellationToken);
        }
    }
}