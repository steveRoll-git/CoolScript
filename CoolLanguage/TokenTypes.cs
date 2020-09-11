using System.Collections.Generic;

namespace CoolScript
{
    enum TokenType
    {
        None, // should only be used by the lexer, or when the token is invalid 
        Keyword,
        Identifier,
        Number,
        String,
        Punctuation,

        BinaryOperator,
        UnaryOperator,

        EndOfFile,
    }

    enum Precedence
    {
        LogicOr,
        LogicAnd,
        Equality,
        Comparison,
        Concat,
        Additive,
        Multiplicative,
        Power,
        Invalid = -1
    }

    enum Associativity
    {
        Left,
        Right
    }

    struct Token
    {
        public TokenType type;
        public string value;
        public int line;

        public bool valid => type != TokenType.None;

        public Token(TokenType t)
        {
            type = t;
            value = "";
            line = 0;
        }

        public Token(TokenType t, string v)
        {
            type = t;
            value = v;
            line = 0;
        }

        public Token(TokenType t, string v, int l)
        {
            type = t;
            value = v;
            line = l;
        }

        public bool compare(Token t2)
        {
            return type == t2.type && (t2.value == "" || value == t2.value);
        }

        public int precedence
        {
            get
            {
                switch (value)
                {
                    case "||":
                        return (int)Precedence.LogicOr;
                    case "&&":
                        return (int)Precedence.LogicAnd;
                    case "==":
                    case "!=":
                        return (int)Precedence.Equality;
                    case ">":
                    case "<":
                    case ">=":
                    case "<=":
                        return (int)Precedence.Comparison;
                    case "..":
                        return (int)Precedence.Concat;
                    case "+":
                    case "-":
                        return (int)Precedence.Additive;
                    case "*":
                    case "/":
                    case "%":
                        return (int)Precedence.Multiplicative;
                    case "^":
                        return (int)Precedence.Power;
                }
                return (int)Precedence.Invalid;
            }
            
        }

        public Associativity associativity
        {
            get
            {
                switch (value)
                {
                    case "^":
                        return Associativity.Right;
                }
                return Associativity.Left;
            }
        }

        static Dictionary<string, VM.InstructionType> binaryInstructions = new Dictionary<string, VM.InstructionType>
        {
            {"+", VM.InstructionType.Add },
            {"-", VM.InstructionType.Sub },
            {"*", VM.InstructionType.Mul },
            {"/", VM.InstructionType.Div },
            {"%", VM.InstructionType.Mod },
            {"^", VM.InstructionType.Pow },
            {"..", VM.InstructionType.Concat },
            {">", VM.InstructionType.Greater },
            {"<", VM.InstructionType.Less },
            {">=", VM.InstructionType.GEqual },
            {"<=", VM.InstructionType.LEqual },
            {"==", VM.InstructionType.Equal },
            {"!=", VM.InstructionType.NEqual },
            {"&&", VM.InstructionType.And },
            {"||", VM.InstructionType.Or }
        };

        public VM.InstructionType getBinaryInstruction()
        {
            if (binaryInstructions.ContainsKey(value))
                return binaryInstructions[value];

            return VM.InstructionType.Add; // will probably cause annoying bugs
        }

        public override string ToString()
        {
            return (type == TokenType.EndOfFile || type == TokenType.String && (value.Length >= 10 || value.Contains("\n"))) ? "<" + type + ">" : "'" + value + "'";
        }

        public static Token Identifier = new Token(TokenType.Identifier);
        public static Token Number = new Token(TokenType.Number);
        public static Token String = new Token(TokenType.String);
        public static Token UnaryOperator = new Token(TokenType.UnaryOperator);
        public static Token EOF = new Token(TokenType.EndOfFile);
    }
}