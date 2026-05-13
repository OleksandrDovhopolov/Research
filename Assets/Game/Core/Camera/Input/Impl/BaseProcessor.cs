using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace InputSystem
{
    public class BaseProcessor
    {
        public static (bool isScreen, bool isWorld) IsUIPressed(out List<Transform> components)
        {
            components = new List<Transform>();
            var result = (false, false);
            var pointPosition = Vector2.zero;
            
#if !UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                pointPosition = new Vector2(Input.GetTouch(0).position.x, Input.GetTouch(0).position.y);
            }
            else
            {
                return result;
            }
#else
            pointPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#endif
            var eventDataCurrentPosition = new PointerEventData(EventSystem.current)
            {
                position = pointPosition
            };

            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventDataCurrentPosition, raycastResults);
            
            foreach (var raycastResult in raycastResults)
            {
                var canvas = raycastResult.module.GetComponent<Canvas>();
                
                components.Add(canvas.transform);
                
                if (canvas == null) continue;

                if (canvas.renderMode == RenderMode.WorldSpace)
                {
                    result.Item2 = true;
                }
                
                if (canvas.renderMode is RenderMode.ScreenSpaceCamera or RenderMode.ScreenSpaceOverlay)
                {
                    result.Item1 = true;
                }
            }
            
            
            return result;
        }
    }
}