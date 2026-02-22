using UnityEngine;

namespace core
{
    [CreateAssetMenu(fileName = "NewCardAnimationConfig", menuName = "Config/NewCardAnimationConfig")]
    public class NewCardAnimationConfig : ScriptableObject
    {
        public int WindowCloseDelayMilliseconds = 500; // Milliseconds wait to close window
        public float NewCardHideDelay = 2f; // play Hide method for new cards after not new cards
        
        public float CardMoveDuration = 0.5f; // cards move duration from pack to screen positions
        public float FlipDuration = 0.5f; // card flip duration
        
        public float PointViewAnimationDuration = 0.4f; //show/hide CardsCollectionPointsView animation duration 
        
        public float PointViewScaleDuration = 1f;
        public float PointViewMoveDelay = 0.5f;
        public float PointViewMoveDuration = 1f;
        public float PointViewMoveRotationSpeed = 360f;
    }
}
