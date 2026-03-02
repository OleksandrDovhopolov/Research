using DG.Tweening;
using UnityEngine;

namespace core
{
    [CreateAssetMenu(fileName = "NewCardAnimationConfig", menuName = "Config/NewCardAnimationConfig")]
    public class NewCardAnimationConfig : ScriptableObject
    {
        [Header("Open Sequence — Mock Cards Move")]
        [Tooltip("Duration for mock cards to move from pack to card slots")]
        public float CardMoveDuration = 0.4f;
        public Ease CardMoveEase = Ease.OutQuart;

        [Header("Open Sequence — Card Flip")]
        [Tooltip("Duration for the mock half-flip (0° → 90°)")]
        public float FlipMockHalfDuration = 0.25f;
        public Ease FlipMockEase = Ease.InQuad;
        [Tooltip("Duration for the real card half-flip (90° → 0°)")]
        public float FlipCardHalfDuration = 0.3f;
        public Ease FlipCardEase = Ease.OutBack;
        [Tooltip("Scale punch strength when the real card is revealed (0 = no punch)")]
        public float FlipScalePunchStrength = 0.08f;
        [Tooltip("Duration of the scale punch after card reveal")]
        public float FlipScalePunchDuration = 0.3f;

        [Header("Close Sequence — Points Container Show/Hide")]
        [Tooltip("Duration for the points container to slide in/out")]
        public float PointViewAnimationDuration = 0.35f;
        public Ease PointViewShowEase = Ease.OutBack;
        public Ease PointViewHideEase = Ease.InBack;

        [Header("Close Sequence — New Card Hide")]
        [Tooltip("Delay (seconds) before new cards start hiding")]
        public float NewCardHideDelay = 1.5f;

        [Header("Close Sequence — Duplicate Card Point Star")]
        [Tooltip("Duration for the point star to scale up from zero")]
        public float PointViewScaleDuration = 0.5f;
        public Ease PointViewScaleEase = Ease.OutBack;
        [Tooltip("Pause after scale-up before the star starts moving")]
        public float PointViewMoveDelay = 0.3f;
        [Tooltip("Duration for the star to fly to the points container")]
        public float PointViewMoveDuration = 0.6f;
        public Ease PointViewMoveEase = Ease.InQuad;
        [Tooltip("Rotation speed (degrees/sec) of the star while flying")]
        public float PointViewMoveRotationSpeed = 540f;
        [Tooltip("Fraction of move duration over which the points text fades out (0–1)")]
        [Range(0f, 1f)]
        public float PointViewFadeDurationRatio = 0.5f;
        public Ease PointViewFadeEase = Ease.OutQuad;

        [Header("Close Sequence — Window Close")]
        [Tooltip("Final delay (ms) after all animations before the window closes")]
        public int WindowCloseDelayMilliseconds = 400;
    }
}
