using System.Reflection;

namespace CsvLoader.Editor
{
    /// <summary>
    /// Дополнительный binder, благодаря которому можно задать соответствение поля и csv столбца
    /// </summary>
    /// <example>
    /// Binder можно использовать для того, чтоб определить связь мержу атрибутом поля и столбцом в таблице
    /// </example>
    public interface IParsingColumnBinder
    {
        bool HaveBind(string tableFieldName, MemberInfo memberInfo);
    }
}