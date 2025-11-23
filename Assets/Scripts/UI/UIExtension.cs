using System;
using UISystem;

namespace core
{
    public static class UIExtension
    {
        public static WindowAttribute GetWindowAttribute<T>() where T : IWindowController
        {
            if (Attribute.IsDefined(typeof(T), typeof(WindowAttribute)))
            {
                return Attribute.GetCustomAttribute(typeof(T), typeof(WindowAttribute)) as WindowAttribute;
            }
            return null;
        }
        
        public static WindowAttribute GetWindowAttribute(this IWindowController window)
        {
            return GetWindowAttribute(window.GetType());
        }
        
        public static WindowAttribute GetWindowAttribute(Type window)
        {
            if (Attribute.IsDefined(window, typeof(WindowAttribute)))
            {
                return Attribute.GetCustomAttribute(window, typeof(WindowAttribute)) as WindowAttribute;
            }
            return null;
        }
        
        public static WindowType GetWindowType(this IWindowController window)
        {
            return  window.GetWindowAttribute().WindowType;
        }
    }
}