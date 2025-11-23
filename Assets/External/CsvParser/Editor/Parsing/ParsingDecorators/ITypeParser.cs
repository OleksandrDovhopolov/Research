using System;

namespace core
{
    /// <summary>
    /// Интерфейс декоратора парсинга типа
    /// </summary>
    public interface ITypeParser
    {
        /// <summary>
        /// Парсер, который будет срабатывать при невозможности спарсить обьект текущим
        /// </summary>
        ITypeParser FallbackParser { set; get; }
        
        /// <summary>
        /// Функция парсинга
        /// </summary>
        /// <param name="value">Строковые данные, которые нужно спарсить</param>
        /// <param name="objectType">Тип поля в которое будут сохранены данные</param>
        /// <param name="curParser">Точка входа в текущий обьект парсера, для возможности вложенного парсинга</param>
        /// <returns></returns>
        object PraseObject(string value, Type objectType, ITypeParser curParser);
    }
}