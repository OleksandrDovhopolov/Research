using UnityEngine;

public class AnimationCardOpener : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    public void PlayCardOpen()
    {
        _animator.SetBool("isOpen", true);
    }

    public void PlayCardClosed()
    {
        _animator.SetBool("isOpen", false);
    }
}
