using System;

namespace CoolScript.Lang
{
    public class CompilerException : Exception
    {
        public CompilerException(string details) : base(details)
        {

        }
    }

    public class SyntaxErrorException : CompilerException
    {
        public SyntaxErrorException(int line, string details)
            : base(string.Format("Syntax error on line {0}: {1}", line, details))
        {

        }
    }

    public class ReferenceErrorException : CompilerException
    {
        public ReferenceErrorException(int line, string details)
            : base(string.Format("Reference error on line {0}: {1}", line, details))
        {

        }
    }
}
