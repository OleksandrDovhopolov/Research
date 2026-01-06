using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace core
{
    public class CollectionCardView : MonoBehaviour
    {
        [SerializeField] private Button _cardButton;
        [SerializeField] private TextMeshProUGUI _closedCardName;
        [SerializeField] private TextMeshProUGUI _openCardName;
        [SerializeField] private Image _cardImage;
        [SerializeField] public RectTransform RectTransform;
        
        [SerializeField] private GameObject star1;
        [SerializeField] private GameObject star2;
        [SerializeField] private GameObject star3;
        [SerializeField] private GameObject star4;
        [SerializeField] private GameObject star5;

        public event Action<CollectionCardView> OnCardPressed;
        
        private void Awake()
        {
            _rt = (RectTransform)_openCardContainer.transform;
            //_rt = (RectTransform)transform;
            _parentRt = (RectTransform)_rt.parent;
            _startAnchoredPos = _rt.anchoredPosition;
            _startScale = _rt.localScale;
        }
        
        private void Start()
        {
            if (_cardButton != null)
            { 
                _cardButton.onClick.AddListener(OnCardPressedHandler);
            }
        }

        private void OnCardPressedHandler()
        {
            OnCardPressed?.Invoke(this);
        }
        
        public void SetCardName(string cardName)
        {
            _closedCardName.text = cardName;
            _openCardName.text = cardName;
        }
        
        public void SetStars(int starsCount)
        {
            starsCount = Mathf.Clamp(starsCount, 1, 5);
            
            star1.SetActive(false);
            star2.SetActive(false);
            star3.SetActive(false);
            star4.SetActive(false);
            star5.SetActive(false);

            if (starsCount == 5)
            {
                star5.SetActive(true);
            }
            else
            {
                if (starsCount >= 1) star1.SetActive(true);
                if (starsCount >= 2) star2.SetActive(true);
                if (starsCount >= 3) star3.SetActive(true);
                if (starsCount >= 4) star4.SetActive(true);
            }            
        }
        
        public void SetCardImage(Sprite cardSprite)
        {
            _cardImage.sprite = cardSprite;
        }

        public void SetOpenCardContainerActive(bool isActive)
        {
            _openCardContainer.SetActive(isActive);
        }
        
        [Space, Space, Header("Animations")]
        [SerializeField] private GameObject _closedCardContainer;
        [SerializeField] private GameObject _openCardContainer;
        [SerializeField] private float _animationDuration = 1f;
        [SerializeField] private float _scaleFactor = 1.5f;
        
        public float AnimationDuration => _animationDuration;
        
        private RectTransform _rt;
        private RectTransform _parentRt;
        private Vector2 _startAnchoredPos;
        private Vector3 _startScale;
        private Coroutine _moveRoutine;
        private Coroutine _scaleRoutine;

        public void PlayCardPreview(Vector2 targetPosition)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);

            //Vector2 screenCenter = GetScreenCenterAnchoredPos();

            _moveRoutine = StartCoroutine(AnimateAnchoredPos(_rt.anchoredPosition, targetPosition, _animationDuration));
            _scaleRoutine = StartCoroutine(AnimateScale(_rt.localScale, _startScale * _scaleFactor, _animationDuration));
        }

        public void HideCard(Vector2 targetPosition)
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);

            //_moveRoutine = StartCoroutine(AnimateAnchoredPos(_rt.anchoredPosition, _startAnchoredPos, _animationDuration));
            _moveRoutine = StartCoroutine(AnimateAnchoredPos(_rt.anchoredPosition, targetPosition, _animationDuration));
            _scaleRoutine = StartCoroutine(AnimateScale(_rt.localScale, _startScale, _animationDuration));
        }

        private Vector2 GetScreenCenterAnchoredPos()
            {
                var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRt, screenCenter, null, out var localPoint); 
        
                return localPoint;
            }
            
        IEnumerator AnimateAnchoredPos(Vector2 from, Vector2 to, float duration)
        {
            if (duration <= 0f)
            {
                _rt.anchoredPosition = to;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime; // чтобы не зависело от Time.timeScale
                float k = Mathf.Clamp01(t / duration);
                _rt.anchoredPosition = Vector2.Lerp(from, to, k);
                yield return null;
            }

            _rt.anchoredPosition = to;
        }

        IEnumerator AnimateScale(Vector3 from, Vector3 to, float duration)
        {
            if (duration <= 0f)
            {
                _rt.localScale = to;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                // можно добавить лёгкий "ease out" для красоты
                k = 1f - (1f - k) * (1f - k); // квадратичный ease-out

                _rt.localScale = Vector3.Lerp(from, to, k);
                yield return null;
            }

            _rt.localScale = to;
        }
        
        private void OnDestroy()
        {
            if (_cardButton != null) 
            { 
                _cardButton.onClick.RemoveAllListeners(); 
            }
        }
    }
}