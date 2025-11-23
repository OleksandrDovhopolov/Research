using System;
using System.Collections.Generic;
using System.Reflection;
using CsvLoader.Editor;
using UnityEditor;
using UnityEngine;

namespace CsvLoader.Serialization
{
    [CustomPropertyDrawer(typeof(SerializedType), true)]
    internal class SerializedTypeDrawer : PropertyDrawer
    {
        private List<Type> _types;
        
        FieldInfo _serializedFieldInfo;
        SerializedProperty _property;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _property = property;
            
            _serializedFieldInfo ??= GetFieldInfo(property);
            _types ??= GetSerializedTypeRestriction(_serializedFieldInfo);

            List<GUIContent> typeContent = new List<GUIContent>();
            typeContent.Add(new GUIContent(SerializedType.NullName, "Clear the type."));
            foreach (var type in _types)
                typeContent.Add(new GUIContent(GetDisplayName(type), ""));

            EditorGUI.BeginProperty(position, label, property);
            
            int index = property.GetValue() is SerializedType curSerializetType ? GetIndexForType(curSerializetType.TypeValue) : 0;
            int selectedValue = EditorGUI.Popup(position, label, index, typeContent.ToArray());

            if (selectedValue != index)
            {
                Undo.RecordObject(_property.serializedObject.targetObject, "Set Serialized Type");
                
                var fieldInstanceType = _serializedFieldInfo.FieldType;

                var ienum = fieldInstanceType.GetInterface("IEnumerable`1");

                if (ienum != null)
                {
                    fieldInstanceType = ienum.GetGenericArguments()[0];
                }
                
                var newInstance = (SerializedType)Activator.CreateInstance(fieldInstanceType);
                newInstance.TypeValue = selectedValue == 0 ? null : _types[selectedValue - 1];
                newInstance.ValueChanged = true;

                property.SetValue(newInstance);
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();
                EditorUtility.SetDirty(_property.serializedObject.targetObject);
            }

            EditorGUI.EndProperty();
        }

        private string GetDisplayName(Type type) => type?.Name ?? SerializedType.NullName;

        int GetIndexForType(Type type)
        {
            if (type == null)
                return 0;
            int index = 1;
            foreach (var checkedType in _types)
            {
                if (checkedType == type)
                    break;
                index++;
            }

            return index;
        }

        static FieldInfo GetFieldInfo(SerializedProperty property)
        {
            var o = property.serializedObject.targetObject;
            var t = o.GetType();
            string propertyName = property.name;
            int i = property.propertyPath.IndexOf('.');
            if (i > 0)
                propertyName = property.propertyPath.Substring(0, i);
            return t.GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        }

        private static List<Type> GetSerializedTypeRestriction(FieldInfo fieldInfo)
        {
            var attrs = fieldInfo.GetCustomAttributes(typeof(SerializedTypeRestrictionAttribute), false);
            if (attrs.Length == 0 || !(attrs[0] is SerializedTypeRestrictionAttribute attribute))
                return null;
            
            return ReflectionHelper.GetAllAssignable(attribute.Type);
        }
    }
}
