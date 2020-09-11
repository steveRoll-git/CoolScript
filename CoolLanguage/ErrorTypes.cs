using System;

namespace CoolScript
{
    class SyntaxErrorException : Exception
    {
        public SyntaxErrorException()
        {

        }

        public SyntaxErrorException(int line, string details)
            : base(String.Format("Syntax error on line {0}: {1}", line, details))
        {

        }
    }
}
