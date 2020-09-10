using System;
using System.Collections.Generic;

namespace CoolLanguage
{

    class Lexer
    {
        static Dictionary<string, bool> keywords = new Dictionary<string, bool>
        {
            {"var", true},
            {"function", true },
            {"return", true },

            {"if", true },
            {"else", true },
            {"while", true },

            {"true", true },
            {"false", true },
            {"null", true },
        };

        private int currentPos = 0;

        private string code;

        private int currentLine = 1;

        public bool reachedEnd { get; private set; } = false;

        static bool isNameChar(char c)
        {
            return char.IsLetter(c) || c == '_';
        }

        static bool isAlNum(char c)
        {
            return char.IsDigit(c) || isNameChar(c);
        }

        static bool isBinaryOperator(string c)
        {
            switch (c)
            {
                case "+": case "-": case "*": case "/": case "^": case "%":
                case "..":
                case "&&": case "||":
                case "==": case "!=": case ">": case "<": case ">=": case "<=":
                    return true;
            }
            return false;
        }

        static bool isUnaryOperator(string c)
        {
            switch (c)
            {
                case "!":
                    return true;
            }
            return false;
        }

        static Dictionary<string, bool> operatorGroups = new Dictionary<string, bool>
        {
            {"&&", true },
            {"||", true },
            {"==", true },
            {"!=", true },
            {">=", true },
            {"<=", true },
            {"..", true }
        };

        static Dictionary<char, char> escapeChars = new Dictionary<char, char>
        {
            {'"', '"' },
            {'\'', '\'' },
            {'\\', '\\' },
            {'n', '\n' },
        };

        public Lexer(string theCode)
        {
            code = theCode;
            if (code.Length == 0)
            {
                reachedEnd = true;
            }
        }

        private char currentChar
        {
            get => currentPos < code.Length ? code[currentPos] : '\0';
        }

        private void nextChar()
        {
            if (!reachedEnd)
            {
                currentPos++;
                if (currentPos >= code.Length)
                {
                    reachedEnd = true;
                }
                else if (currentChar == '\n')
                {
                    currentLine++;
                }
            }
        }

        public Token nextToken()
        {
            while (!reachedEnd && (char.IsWhiteSpace(code, currentPos) || currentChar == '\r' || currentChar == '\n')) // get past all the spaces and newlines
            {
                nextChar();
            }

            if (reachedEnd)
            {
                return Token.EOF;
            }

            TokenType doing = TokenType.None;
            string tokenValue = "";

            if (isAlNum(currentChar))
            {
                doing = char.IsDigit(code, currentPos) ? TokenType.Number : TokenType.Identifier;

                do
                {
                    tokenValue += currentChar;
                    nextChar();
                } while (!reachedEnd && (isAlNum(currentChar) || (doing == TokenType.Number && currentChar == '.')));

                if (keywords.ContainsKey(tokenValue))
                {
                    doing = TokenType.Keyword;
                }
            }
            else if (currentChar == '"' || currentChar == '\'')
            {
                char stringStart = currentChar;

                doing = TokenType.String;

                //Console.WriteLine("string start");

                while (true)
                {
                    nextChar();
                    //Console.WriteLine(currentPos);
                    if (reachedEnd)
                    {
                        throw new SyntaxErrorException(currentLine, "unfinished string");
                    }
                    else if (currentChar == stringStart)
                    {
                        nextChar();
                        break;
                    }
                    else if (currentChar == '\\')
                    {
                        nextChar();
                        if (escapeChars.TryGetValue(currentChar, out char escapeChar))
                        {
                            tokenValue += escapeChar;
                        }
                        else
                        {
                            throw new SyntaxErrorException(currentLine, "invalid escape sequence");
                        }
                    }
                    else
                    {
                        tokenValue += currentChar;
                        //Console.WriteLine("!" + tokenValue);
                    }
                }
            }
            else
            {
                do
                {
                    tokenValue += currentChar;
                    nextChar();
                } while (operatorGroups.ContainsKey(code[currentPos - 1].ToString() + currentChar));

                doing = isUnaryOperator(tokenValue) ? TokenType.UnaryOperator : (isBinaryOperator(tokenValue) ? TokenType.BinaryOperator : TokenType.Punctuation);
            }

            return new Token(doing, tokenValue, currentLine);
        }

        private char Peek()
        {
            return currentPos < code.Length - 1 ? code[currentPos + 1] : '\0';
        }
    }
}
