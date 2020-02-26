using System;
using System.Collections.Generic;

namespace CoolLanguage
{

    class Lexer
    {
        static Dictionary<string, bool> keywords = new Dictionary<string, bool>
        {
            {"var", true},
            {"true", true },
            {"false", true }
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

        public Token nextToken()
        {
            TokenType doing = TokenType.None;
            string tokenValue = "";

            if (currentPos == code.Length)
            {
                reachedEnd = true;
            }

            if (reachedEnd)
            {
                doing = TokenType.EndOfFile;
            }
            else
            {
                while (char.IsWhiteSpace(code, currentPos) || currentChar == '\r' || currentChar == '\n') // get past all the spaces and newlines
                {
                    if (currentChar == '\n')
                        currentLine++;

                    currentPos++;

                    if (currentPos == code.Length)
                    {
                        doing = TokenType.EndOfFile;
                        reachedEnd = true;
                        break;
                    }
                }

                if (doing != TokenType.EndOfFile)
                {
                    if (isAlNum(currentChar))
                    {
                        doing = char.IsDigit(code, currentPos) ? TokenType.Number : TokenType.Identifier;

                        do
                        {
                            tokenValue += currentChar;
                            currentPos++;
                        } while (currentPos < code.Length && (isAlNum(currentChar) || (doing == TokenType.Number && currentChar == '.')));

                        if (keywords.ContainsKey(tokenValue))
                        {
                            doing = TokenType.Keyword;
                        }
                    }
                    else if (currentChar == '"' || currentChar == '\'')
                    {
                        char stringStart = currentChar;

                        doing = TokenType.String;
                        bool nextRaw = false;

                        //Console.WriteLine("string start");

                        while (true)
                        {
                            currentPos++;
                            //Console.WriteLine(currentPos);
                            if (currentPos >= code.Length)
                            {
                                throw new SyntaxErrorException(currentLine, "unfinished string");
                            }
                            else if (currentChar == stringStart && !nextRaw)
                            {
                                currentPos++;
                                break;
                            }
                            else if (currentChar == '\\' && !nextRaw)
                            {
                                nextRaw = true;
                            }
                            else
                            {
                                tokenValue += currentChar;
                                //Console.WriteLine("!" + tokenValue);
                                nextRaw = false;
                            }
                        }
                    }
                    else
                    {
                        do
                        {
                            tokenValue += currentChar;
                            currentPos++;
                        } while (operatorGroups.ContainsKey(code[currentPos - 1].ToString() + currentChar));

                        doing = isUnaryOperator(tokenValue) ? TokenType.UnaryOperator : (isBinaryOperator(tokenValue) ? TokenType.BinaryOperator : TokenType.Punctuation);
                    }
                }
            }

            return new Token(doing, tokenValue, currentLine);
        }

        private char Peek()
        {
            return currentPos < code.Length - 1 ? code[currentPos + 1] : '\0';
        }
    }
}
