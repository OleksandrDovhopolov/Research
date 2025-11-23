using System;

namespace core
{
    internal class ParsingSkipException : Exception
    {
        public ParsingSkipException(string message) : base(message)
        {
        }
    }
}