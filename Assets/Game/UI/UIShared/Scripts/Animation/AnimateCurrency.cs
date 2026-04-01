using System;
using CoreResources;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UIShared
{
    public class ArgAnimateCurrency
    {
        public Vector3 StartScreenPosition { get; }
        public string ItemType { get; }
        public int Amount { get; }
        public Sprite Sprite { get; }

        public ArgAnimateCurrency(Vector3 startScreenPosition, string itemType, int amount, Sprite sprite)
        {
            Amount = amount;
            Sprite = sprite;
            ItemType = itemType;
            StartScreenPosition = startScreenPosition;
        }
    }
    
    public class AnimateCurrency : MonoBehaviour
    {
        private const int ViewsCount = 6;
        
        [SerializeField] private UIListPool<SharedAnimationView> _animationViewsPool;
        [SerializeField] private AnimationSettings _animationSettings;
        
        public Tween Animate(ArgAnimateCurrency animationArgs, Action onAnimationCompleted)
        {
            var delay = 0f;
            var destination = AnimationTargets.GetTargetPosition(animationArgs.ItemType);
            var animation = DOTween.Sequence().SetUpdate(true);

            var viewsCount = animationArgs.Amount == 1 ? 1 : ViewsCount;
                
            for (var i = 0; i < viewsCount; i++)
            {
                var parameters = _animationSettings.GetResourceAnimationParameters(animationArgs.ItemType);
                
                animation.Insert(delay, AnimateCurvedParticle(animationArgs.StartScreenPosition, destination, animationArgs.Sprite, parameters, onAnimationCompleted));
                
                delay += parameters.SpawnDelay;
            }

            return animation;
        }
        
        private Tween AnimateCurvedParticle(Vector3 startPosition, Vector3 finishPosition, Sprite sprite, ParticleAnimationParameters parameters, Action onAnimationCompleted)
        {
            var particle = _animationViewsPool.GetNext();
            particle.gameObject.SetActive(false);
            particle.SetSprite(sprite);
            
            var pt = particle.transform;
            pt.position = startPosition;
            pt.localScale = parameters.StartScaleValue;

            var duration = parameters.CurveAnimTime;
            var direction = pt.position.x > 0;

            var particleAnimation = DOTween.Sequence();

            particleAnimation.AppendCallback(() =>
            {
                particle.gameObject.SetActive(true);
                particle.transform.SetAsFirstSibling();
            });

            particleAnimation.Append(particle.gameObject.transform
                .DOPath(
                    GetPath(particle.transform.position, finishPosition, direction, parameters.OverwhelmMin,
                        parameters.OverwhelmMax), duration, PathType.CubicBezier)
                .SetEase(parameters.FlightEase));

            particleAnimation.Join(DOVirtual.Float(0f, 1f, duration,
                    value => particle.transform.localScale =
                        parameters.ScaleEase.Evaluate(value) * parameters.StartScaleValue)
                .SetEase(Ease.Linear));

            particleAnimation.OnComplete(() =>
            {
                onAnimationCompleted?.Invoke();
                _animationViewsPool.Return(particle);
            });

            return particleAnimation;
        }
        
        private Vector3[] GetPath(Vector2 startPosition, Vector2 endPosition, bool left, float overwhelmMin, float overwhelmMax)
        {
            var random = Random.Range(0.2f, 0.9f);
            random *= left ? -1 : 1;

            var distX = Math.Abs(endPosition.x - startPosition.x);
            var distY = Math.Abs(endPosition.y - startPosition.y);

            var horizontal = distX > distY;
            var maxDist = horizontal ? distX : distY;
        
            var a = random * 200;
            var b = maxDist * 0.5f;
            var b1 = maxDist * Random.Range(overwhelmMin, overwhelmMax);

            var p3 = endPosition;

            if (horizontal)
            {
                p3.x += -b;
                p3.y = startPosition.y + a * 2f;
            }
            else
            {
                p3.x = startPosition.x + a * 2f;
                p3.y += -b;
            }

            return new Vector3[]
            {
                endPosition,
                startPosition + new Vector2(a * 2f, b1),
                p3
            };
        }
    }
}