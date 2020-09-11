using System;

namespace CoolScript.Lang
{
    class SyntaxErrorException : Exception
    {
        public SyntaxErrorException()
        {

        }

        public SyntaxErrorException(int line, string details)
            : base(string.Format("Syntax error on line {0}: {1}", line, details))
        {

        }
    }
}
