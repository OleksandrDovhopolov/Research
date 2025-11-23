using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CsvLoader.Editor;
using UnityEngine;

namespace core
{
    public abstract class BaseCsvSaveStrategy : CsvSaveStrategy
    {
        private readonly Lazy<List<IParsingColumnBinder>> _parsingColumnBinders = new(ReflectionHelper.GetAllDerivedInstances<IParsingColumnBinder>);
        
        private static readonly List<ITypeParser> Parsers = new()
        {
            new TypeParserBase(),
            new StringTypeParser(),
            new EnumTypeParser(),
            new FloatTypeParser(),
            new BoolTypeParser(),
            new ListTypeParser()
        };
        
         protected IList ParseSheetByTypeName(IList<IList<string>> table, string typeName)
        {
            var elementType = ReflectionHelper.FindType(typeName);

            if (elementType == null)
            {
                throw new InvalidOperationException($"Type name {typeName} not found. Set correct TypeName in CsvSheetInfo ScriptableObject. Should be class name with field as in google sheet");
            }
            
            var properties = new List<Tuple<TableFieldInfo, int>>();

            //перебираем строчки таблицы сверху вниз (не считая первой строчки, в которую записаны названия полей)
            var allMembers = elementType.GetMembers(ReflectionHelper.InstanceFieldsAccess).Where(info => info is FieldInfo || info is PropertyInfo);
            foreach (var memberInfo in allMembers)
            {
                for (int i = 0; i < table[0].Count; i++)
                {
                    if (memberInfo.Name == GetCorrectName(table[0][i]) || _parsingColumnBinders.Value.Any(binder => binder.HaveBind(GetCorrectName(table[0][i]), memberInfo)))
                    {
                        //в список свойств добавили свойство и индекс столбца, из которого брать значение для этого свойства
                        properties.Add(Tuple.Create(new TableFieldInfo(memberInfo), i));
                    }
                }
            }
            
            //создали массив элементов
            var elementsListType = typeof(List<>).MakeGenericType(elementType);
            
            var list = (IList)Activator.CreateInstance(elementsListType);
            var curTypeParser = GenerateCurParser();

            //проходим по таблице сверху вниз
            for (int i = 1; i < table.Count; i++)
            {
                //создали новый элемент
                var newElement = Activator.CreateInstance(elementType);

                foreach (var v in properties) //для каждого сохраненного свойства
                {
                    var tableField = v.Item1;
                    var colIndex = v.Item2;
                    try
                    {
                        var convertedRow =
                            curTypeParser.PraseObject(table[i][colIndex], tableField.FieldType, curTypeParser);

                        //установили в свойство значение из i-й строчки таблицы
                        v.Item1.SetValue(newElement, convertedRow);
                    }
                    catch (ParsingSkipException skipException)
                    {
                        Debug.LogWarning($"Error while parsing row {i} ({tableField.FieldName}): {skipException.Message}. Row will be skipped.");
                        newElement = null;
                        break;
                    }
                    catch 
                    {
                        Debug.LogError($"Error in parsing row {i} column {tableField.FieldName}");
                        throw;
                    }
                }

                if (newElement != null)
                {
                    //установили значение в массив
                    list.Add(newElement);
                }
            }

            return list;
        }
        
        /// <summary>
        /// Генерирует парсер заполняя поля fallback для парсеров предыдущими в базе
        /// </summary>
        /// <returns>Точка входа в парсер</returns>
        private static ITypeParser GenerateCurParser()
        {
            for (var i = Parsers.Count - 1; i >= 1; i--)
            {
                Parsers[i].FallbackParser = Parsers[i - 1];
            }

            return Parsers.Last();
        }
        
        private static string GetCorrectName(string title)
        {
            var index = title.IndexOf(' ');
            return index < 0 ? title : title.Substring(0, index);
        }
        
        private class TableFieldInfo
        {
            private readonly FieldInfo _fieldInfo;
            private readonly PropertyInfo _propertyInfo;
            
            public TableFieldInfo(MemberInfo memberInfo)
            {
                _fieldInfo = memberInfo as FieldInfo;
                _propertyInfo = memberInfo as PropertyInfo;
            }
            
            public string FieldName => _fieldInfo?.Name ?? _propertyInfo.Name;

            public Type FieldType => _fieldInfo?.FieldType ?? _propertyInfo.PropertyType;

            public void SetValue(object target, object val)
            {
                if (_fieldInfo != null)
                {
                    _fieldInfo.SetValue(target, val);
                    return;
                }
                
                _propertyInfo.SetValue(target, val);
            }
        }
    }
}