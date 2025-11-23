using System.Collections.Generic;
using CsvLoader.Serialization;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace core
{
    [CustomPropertyDrawer(typeof(SerializedDictionaryDrawable), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        private const string ElemsPropertyName = "_elements";
        private const string KeyPropertyName = "_key";
        private const string ValuePropertyName = "_value";
        private static float InfoBoxLineHeight => EditorGUIUtility.singleLineHeight * 2f;

        private ReorderableList _reorderableList;
        private Dictionary<object, int> _activeKeys = new();
        
        private void InitReorderableList(SerializedProperty property)
        {
            SerializedProperty arrayProperty = property.FindPropertyRelative(ElemsPropertyName);
            _reorderableList = new ReorderableList(property.serializedObject, arrayProperty, true, true, true, true);

            // Конфігуруємо відображення елементів масиву
            _reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, property.displayName);
                
            };
            _reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = arrayProperty.GetArrayElementAtIndex(index);

                // Отримуємо ключ та значення з елемента
                SerializedProperty key = element.FindPropertyRelative(KeyPropertyName);
                SerializedProperty value = element.FindPropertyRelative(ValuePropertyName);

                // Конфігуруємо розміщення стовпців для ключа та значення
                float columnWidth = rect.width * 0.5f;
                Rect keyRect = new Rect(rect.x, rect.y, columnWidth, EditorGUIUtility.singleLineHeight);
                Rect valueRect = new Rect(rect.x + columnWidth, rect.y, columnWidth, EditorGUIUtility.singleLineHeight);

                var originalColor = GUI.color;
                var keyValue = key.GetValue();

                if (_activeKeys.TryGetValue(keyValue, out var cachedIndex) && cachedIndex != index)
                {
                    GUI.color = Color.red;
                }
                
                EditorGUI.PropertyField(keyRect, key, GUIContent.none);
                EditorGUI.PropertyField(valueRect, value, GUIContent.none);
                
                GUI.color = originalColor;
            };

            // Конфігуруємо відображення кнопки "+"
            _reorderableList.onAddCallback = (ReorderableList list) =>
            {
                int newIndex = list.serializedProperty.arraySize;
                list.serializedProperty.arraySize++;
                list.index = newIndex;
            };
        }

        private void CacheActiveKeys()
        {
            if (_reorderableList == null) return;
            
            _activeKeys ??= new Dictionary<object, int>();
            _activeKeys.Clear();

            for (int i = 0; i < _reorderableList.count; i++)
            {
                var elem = _reorderableList.serializedProperty.GetArrayElementAtIndex(i);
                var key = elem.FindPropertyRelative(KeyPropertyName).GetValue();

                if (!_activeKeys.ContainsKey(key))
                {
                    _activeKeys[key] = i;
                }
            }
        }

        private bool ContainsDuplicateKeys()
        {
            if (_activeKeys == null || _reorderableList == null) return false;

            return _activeKeys.Count != _reorderableList.count;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CacheActiveKeys();

            EditorGUI.BeginProperty(position, label, property);
            
            if (ContainsDuplicateKeys())
            {
                // Визначаємо розмір для інфо-боксу
                var infoBoxRect = new Rect(position.x, position.y, position.width, InfoBoxLineHeight);

                EditorGUI.HelpBox(infoBoxRect, $"Dictionary {property.name} contains duplicate keys.", MessageType.Error);

                // Переміщуємо список нижче інфо-боксу
                position.y += InfoBoxLineHeight;
                position.height -= InfoBoxLineHeight;
            }
            
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, property.displayName);

            // Відображаємо список, якщо фолдаут розгорнутий
            if (property.isExpanded)
            {
                position.y += EditorGUIUtility.singleLineHeight;
                position.height -= EditorGUIUtility.singleLineHeight;

                if (_reorderableList == null)
                {
                    InitReorderableList(property);
                }
                
                _reorderableList.DoList(position);
            }

            EditorGUI.EndProperty();

        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (_reorderableList == null)
            {
                InitReorderableList(property);
            }
            
            float totalHeight = EditorGUIUtility.singleLineHeight;
            
            // Обчислюємо висоту інфо-боксу та списку
            totalHeight += ContainsDuplicateKeys() ? InfoBoxLineHeight : 0;
            
            // Обчислюємо висоту списку, якщо фолдаут розгорнутий
            if (property.isExpanded)
            {
                totalHeight += _reorderableList.GetHeight();
            }
            
            return totalHeight;
        }
    }
}