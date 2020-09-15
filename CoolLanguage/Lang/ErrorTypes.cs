using System;

namespace CoolScript.Lang
{
    public class SyntaxErrorException : Exception
    {
        public SyntaxErrorException()
        {

        }

        public SyntaxErrorException(int line, string details)
            : base(string.Format("Syntax error on line {0}: {1}", line, details))
        {

        }
    }

    public class ReferenceErrorException : Exception
    {
        public ReferenceErrorException()
        {

        }

        public ReferenceErrorException(int line, string details)
            : base(string.Format("Reference error on line {0}: {1}", line, details))
        {

        }
    }
}
