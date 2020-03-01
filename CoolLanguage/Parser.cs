using System;
using System.Collections.Generic;

using CoolLanguage.VM;

namespace CoolLanguage
{
    enum TreeType
    {
        None,

        PrimitiveNumber,
        PrimitiveString,
        PrimitiveBoolean,
        PrimitiveNull,

        BinaryOperator,
        UnaryExpression,

        GlobalValue,
        GlobalAssignment,

        LocalValue,
        LocalAssignment,

        ObjectIndexValue,
        ObjectIndexAssignment,
        FunctionCall,

        ReturnStatement,
        Block,

        CreateTable,
    }

    enum Associativity
    {
        Left,
        Right
    }

    abstract class Tree
    {
        public TreeType type
        {
            get;
            protected set;
        } = TreeType.None;

        public abstract VMInstruction[] GetInstructions();
    }

    class NumberTree : Tree
    {
        public string value;

        public NumberTree(string v)
        {
            type = TreeType.PrimitiveNumber;
            value = v;
        }

        override public VMInstruction[] GetInstructions()
        {
            try
            {
                double theNumber = double.Parse(value);
                return new VMInstruction[] { new VMInstruction(InstructionType.PushNumber, theNumber) };
            }
            catch (FormatException)
            {
                throw new SyntaxErrorException(0, "Malformed number: '" + value + "'");
            }
        }
    }

    class StringTree : Tree
    {
        public string value;

        public StringTree(string v)
        {
            type = TreeType.PrimitiveString;
            value = v;
        }

        public override VMInstruction[] GetInstructions()
        {
            return new VMInstruction[] { new VMInstruction(InstructionType.PushString, value) };
        }
    }

    class BoolTree : Tree
    {
        public bool value;

        public BoolTree(bool v)
        {
            type = TreeType.PrimitiveBoolean;

            value = v;
        }

        public override VMInstruction[] GetInstructions()
        {
            return new VMInstruction[] { new VMInstruction(InstructionType.PushBool, value) };
        }
    }

    class NullTree : Tree
    {
        public NullTree()
        {
            type = TreeType.PrimitiveNull;
        }

        public override VMInstruction[] GetInstructions()
        {
            return new VMInstruction[] { new VMInstruction(InstructionType.PushNull) };
        }
    }

    class UnaryExpressionTree : Tree
    {
        public string op;
        public Tree value;

        public UnaryExpressionTree(string o, Tree v)
        {
            type = TreeType.UnaryExpression;
            op = o;
            value = v;
        }

        override public VMInstruction[] GetInstructions()
        {
            VMInstruction[] valueInstructions = value.GetInstructions();

            VMInstruction[] toReturn = new VMInstruction[valueInstructions.Length + 1];

            Array.Copy(valueInstructions, toReturn, valueInstructions.Length);

            InstructionType instruction = InstructionType.Negate;

            switch (op)
            {
                case "-":
                    instruction = InstructionType.Negate;
                    break;
                case "!":
                    instruction = InstructionType.Not;
                    break;
            }

            toReturn[toReturn.Length - 1] = new VMInstruction(instruction);

            return toReturn;
        }
    }

    class BinaryOperatorTree : Tree
    {
        public Token op;

        public Tree left;
        public Tree right;

        public BinaryOperatorTree(Token o, Tree l, Tree r)
        {
            type = TreeType.BinaryOperator;
            op = o;
            left = l;
            right = r;
        }

        override public VMInstruction[] GetInstructions()
        {
            VMInstruction[] leftInstructions = left.GetInstructions();
            VMInstruction[] rightInstructions = right.GetInstructions();

            VMInstruction[] toReturn = new VMInstruction[leftInstructions.Length + rightInstructions.Length + 1];

            Array.Copy(leftInstructions, 0, toReturn, 0, leftInstructions.Length);
            Array.Copy(rightInstructions, 0, toReturn, leftInstructions.Length, rightInstructions.Length);

            toReturn[toReturn.Length - 1] = new VMInstruction(op.getBinaryInstruction());

            return toReturn;
        }
    }

    class GlobalValueTree : Tree
    {
        public string name;

        public GlobalValueTree(string n)
        {
            type = TreeType.GlobalValue;

            name = n;
        }

        override public VMInstruction[] GetInstructions()
        {
            return new VMInstruction[] { new VMInstruction(InstructionType.PushGlobal, name) };
        }
    }

    class GlobalAssignmentTree : Tree
    {
        public string varName;
        public Tree value;

        public GlobalAssignmentTree(string name, Tree val)
        {
            type = TreeType.GlobalAssignment;

            varName = name;
            value = val;
        }

        override public VMInstruction[] GetInstructions()
        {
            VMInstruction[] valueInstructions = value.GetInstructions();

            VMInstruction[] toReturn = new VMInstruction[valueInstructions.Length + 1];

            Array.Copy(valueInstructions, toReturn, valueInstructions.Length);

            toReturn[toReturn.Length - 1] = new VMInstruction(InstructionType.PopGlobal, varName);

            return toReturn;
        }
    }

    class LocalValueTree : Tree
    {
        public int functionId;

        public int index;

        public LocalValueTree(int _id, int _index)
        {
            type = TreeType.LocalValue;

            functionId = _id;
            index = _index;
        }

        public override VMInstruction[] GetInstructions()
        {
            return new VMInstruction[] { new VMInstruction(InstructionType.GetLocal, index) };
        }
    }

    class LocalAssignmentTree : Tree
    {
        public int localIndex;
        public Tree value;

        public LocalAssignmentTree(int _localIndex, Tree _value)
        {
            type = TreeType.LocalAssignment;

            localIndex = _localIndex;
            value = _value;
        }

        override public VMInstruction[] GetInstructions()
        {
            VMInstruction[] valueInstructions = value.GetInstructions();

            VMInstruction[] toReturn = new VMInstruction[valueInstructions.Length + 1];

            Array.Copy(valueInstructions, toReturn, valueInstructions.Length);

            toReturn[toReturn.Length - 1] = new VMInstruction(InstructionType.SetLocal, localIndex);

            return toReturn;
        }
    }

    class ObjectIndexValueTree : Tree
    {
        public Tree Object;
        public Tree Index;

        public ObjectIndexValueTree(Tree _object, Tree _index)
        {
            type = TreeType.ObjectIndexValue;

            Object = _object;
            Index = _index;
        }

        public override VMInstruction[] GetInstructions()
        {
            VMInstruction[] objectInstructions = Object.GetInstructions();
            VMInstruction[] indexInstructions = Index.GetInstructions();

            VMInstruction[] toReturn = new VMInstruction[objectInstructions.Length + indexInstructions.Length + 1];

            Array.Copy(objectInstructions, toReturn, objectInstructions.Length);
            Array.Copy(indexInstructions, 0, toReturn, objectInstructions.Length, indexInstructions.Length);

            toReturn[toReturn.Length - 1] = new VMInstruction(InstructionType.GetTable);

            return toReturn;
        }
    }

    class ObjectIndexAssignmentTree : Tree
    {
        public Tree Object;
        public Tree Index;
        public Tree Value;

        public ObjectIndexAssignmentTree(Tree _object, Tree _index, Tree _value)
        {
            type = TreeType.ObjectIndexAssignment;

            Object = _object;
            Index = _index;
            Value = _value;
        }

        public override VMInstruction[] GetInstructions()
        {
            VMInstruction[] objectInstructions = Object.GetInstructions();
            VMInstruction[] indexInstructions = Index.GetInstructions();
            VMInstruction[] valueInstructions = Value.GetInstructions();

            VMInstruction[] toReturn = new VMInstruction[objectInstructions.Length + indexInstructions.Length + valueInstructions.Length + 1];

            Array.Copy(objectInstructions, toReturn, objectInstructions.Length);
            Array.Copy(indexInstructions, 0, toReturn, objectInstructions.Length, indexInstructions.Length);
            Array.Copy(valueInstructions, 0, toReturn, objectInstructions.Length + indexInstructions.Length, valueInstructions.Length);

            toReturn[toReturn.Length - 1] = new VMInstruction(InstructionType.SetTable);

            return toReturn;
        }
    }

    class FunctionCallTree : Tree
    {
        private List<Tree> arguments = new List<Tree>();

        public Tree functionTree;

        public FunctionCallTree(Tree function)
        {
            type = TreeType.FunctionCall;

            functionTree = function;
        }

        public void AddArgument(Tree arg)
        {
            arguments.Add(arg);
        }

        public override VMInstruction[] GetInstructions()
        {
            List<VMInstruction> instructions = new List<VMInstruction>();

            foreach (Tree tree in arguments)
            {
                instructions.AddRange(tree.GetInstructions());
            }

            instructions.AddRange(functionTree.GetInstructions());

            instructions.Add(new VMInstruction(InstructionType.Call, arguments.Count));

            return instructions.ToArray();
        }
    }

    class CreateTableTree : Tree
    {
        public CreateTableTree()
        {
            type = TreeType.CreateTable;
        }

        public override VMInstruction[] GetInstructions()
        {
            return new VMInstruction[] { new VMInstruction(InstructionType.CreateTable) };
        }
    }

    class BlockTree : Tree
    {
        public List<Tree> statements = new List<Tree>();

        public int localCount = 0;

        public BlockTree()
        {
            type = TreeType.Block;
        }

        public void AddTree(Tree tree)
        {
            statements.Add(tree);
        }

        public override VMInstruction[] GetInstructions()
        {
            List<VMInstruction> instructionsList = new List<VMInstruction>();
            for (int i = 0; i < statements.Count; i++)
            {
                instructionsList.AddRange(statements[i].GetInstructions());
            }

            return instructionsList.ToArray();
        }
    }

    class Scope
    {
        public Dictionary<string, int> localVariables = new Dictionary<string, int>();

        public int localCount = 0;
        public int maxLocalCount = 0;

        public int functionId;

        //public FunctionPrototype prototype;

        public Scope(int _localCount = 0)
        {
            localCount = _localCount;
        }
    }

    class Parser
    {
        public Lexer lexer;

        private Token curToken;

        static Token lParen = new Token(TokenType.Punctuation, "(");
        static Token rParen = new Token(TokenType.Punctuation, ")");

        static Token lSquare = new Token(TokenType.Punctuation, "[");
        static Token rSquare = new Token(TokenType.Punctuation, "]");

        static Token lCurly = new Token(TokenType.Punctuation, "{");
        static Token rCurly = new Token(TokenType.Punctuation, "}");

        static Token minus = new Token(TokenType.BinaryOperator, "-");
        static Token equals = new Token(TokenType.Punctuation, "=");
        static Token dot = new Token(TokenType.Punctuation, ".");
        static Token semicolon = new Token(TokenType.Punctuation, ";");
        static Token comma = new Token(TokenType.Punctuation, ",");

        static Token kTrue = new Token(TokenType.Keyword, "true");
        static Token kFalse = new Token(TokenType.Keyword, "false");
        static Token kNull = new Token(TokenType.Keyword, "null");

        private List<Scope> scopes = new List<Scope>();

        private int functionIdCount = 0;

        private List<FunctionPrototype> prototypes = new List<FunctionPrototype>();

        //////////

        static Associativity getAssociativity(Token op)
        {
            switch (op.value)
            {
                case "^":
                    return Associativity.Right;
            }
            return Associativity.Left;
        }
        
        private void nextToken()
        {
            curToken = lexer.nextToken();
        }

        private Token accept(Token t)
        {
            if (curToken.compare(t))
            {
                Token current = curToken;
                nextToken();
                return current;
            }
            return new Token();
        }

        private Token expect(Token t)
        {
            Token token = accept(t);
            if (token.valid)
                return token;

            throw new SyntaxErrorException(curToken.line, "expected " + t + ", got " + curToken);
        }

        private Tree GetVariableFromName(string name)
        {
            for (int i = scopes.Count - 1; i >= 0; i--)
            {
                if (scopes[i].localVariables.ContainsKey(name))
                {
                    return new LocalValueTree(scopes[i].functionId, scopes[i].localVariables[name]);
                }
            }
            return new GlobalValueTree(name);
        }

        //==========

        private Tree ParseIndexOrCall(Tree theObject = null)
        {
            if (theObject == null)
            {
                if (curToken.type == TokenType.Identifier)
                {
                    theObject = GetVariableFromName(curToken.value);
                    if (theObject.type == TreeType.LocalValue)
                    {
                        if ((theObject as LocalValueTree).functionId != scopes[scopes.Count - 1].functionId)
                        {
                            throw new SyntaxErrorException(curToken.line, "upvalues are not supported");
                        }
                    }
                    nextToken();
                }
                else if (accept(lParen).valid)
                {
                    theObject = ParseExpression();
                    expect(rParen);
                }
                else
                {
                    throw new SyntaxErrorException(curToken.line, "Did not expect " + curToken + " here");
                }
            }

            if (accept(dot).valid)
            {
                //index using identifier

                theObject = new ObjectIndexValueTree(theObject, new StringTree(expect(Token.Identifier).value));

                return ParseIndexOrCall(theObject);
            }
            else if (accept(lSquare).valid)
            {
                //index using expression

                Tree index = ParseExpression();

                expect(rSquare);

                theObject = new ObjectIndexValueTree(theObject, index);

                return ParseIndexOrCall(theObject);
            }
            else if (accept(lParen).valid)
            {
                //function call

                FunctionCallTree tree = new FunctionCallTree(theObject);

                if (!accept(rParen).valid)
                {
                    do
                    {
                        tree.AddArgument(ParseExpression());
                    } while (accept(comma).valid);
                    expect(rParen);
                }

                return ParseIndexOrCall(tree);
            }

            return theObject;
        }

        private Tree ParsePrimary()
        {
            Token unaryOp = accept(minus); // we try minus first because it's considered a binary operator
            if (!unaryOp.valid)
                unaryOp = accept(Token.UnaryOperator);

            if (unaryOp.valid)
                return new UnaryExpressionTree(unaryOp.value, ParsePrimary());

            Token num = accept(Token.Number);
            if (num.valid)
                return new NumberTree(num.value);

            Token theString = accept(Token.String);
            if (theString.valid)
                return new StringTree(theString.value);

            Token theBool = accept(kTrue);
            if (!theBool.valid)
                theBool = accept(kFalse);
            if (theBool.valid)
                return new BoolTree(bool.Parse(theBool.value));

            if (accept(kNull).valid)
                return new NullTree();

            Token curly = accept(lCurly);
            if (curly.valid)
            {
                //TODO table definition here
                expect(rCurly);
                return new CreateTableTree();
            }

            /*expect(lParen);
            Tree exp = ParseExpression();
            expect(rParen);*/

            Tree obj = ParseIndexOrCall();
            return obj;
        }

        private Tree ParseExpression1(Tree lhs, int minPrecedence) // https://en.wikipedia.org/wiki/Operator-precedence_parser literally just copied the pseudocode
        {
            Token lookahead = curToken;
            while( lookahead.type == TokenType.BinaryOperator && lookahead.getPrecedence() >= minPrecedence){

                Token op = lookahead;
                nextToken();
                Tree rhs = ParsePrimary();
                lookahead = curToken;
                while(lookahead.type == TokenType.BinaryOperator && ((lookahead.getPrecedence() > op.getPrecedence()) || (getAssociativity(lookahead) == Associativity.Right && lookahead.getPrecedence() == op.getPrecedence())))
                {
                    rhs = ParseExpression1(rhs, lookahead.getPrecedence());
                    lookahead = curToken;
                }

                lhs = new BinaryOperatorTree(op, lhs, rhs);
            }
            return lhs;
        }

        private Tree ParseExpression()
        {
            return ParseExpression1(ParsePrimary(), 0);
        }

        private Tree ParseStatement()
        {
            if (curToken.type == TokenType.Keyword)
            {
                Token keyword = curToken;
                nextToken();

                if (keyword.value == "var")
                {
                    string varName = curToken.value;

                    nextToken();

                    expect(equals);

                    Tree expression = ParseExpression();

                    Scope last = scopes[scopes.Count - 1];

                    last.localVariables.Add(varName, last.localCount);

                    last.localCount++;
                    last.maxLocalCount = Math.Max(last.localCount, last.maxLocalCount);

                    return new LocalAssignmentTree(last.localCount - 1, expression);
                }
            }
            else if (curToken.type == TokenType.Identifier || curToken.compare(lParen))
            {
                Tree thing = ParseIndexOrCall();

                if (thing.type == TreeType.FunctionCall)
                {
                    return thing;
                }
                else
                {
                    // assignment

                    if (thing.type == TreeType.GlobalValue)
                    {
                        expect(equals);

                        Tree value = ParseExpression();

                        return new GlobalAssignmentTree((thing as GlobalValueTree).name, value);
                    }
                    else if (thing.type == TreeType.LocalValue)
                    {
                        expect(equals);

                        Tree value = ParseExpression();

                        return new LocalAssignmentTree((thing as LocalValueTree).index, value);
                    }
                    else if (thing.type == TreeType.ObjectIndexValue)
                    {
                        expect(equals);

                        Tree value = ParseExpression();

                        ObjectIndexValueTree OIVTree = thing as ObjectIndexValueTree;

                        return new ObjectIndexAssignmentTree(OIVTree.Object, OIVTree.Index, value);
                    }
                }
            }
            throw new SyntaxErrorException(curToken.line, "Did not expect " + curToken + " here");
        }

        private BlockTree ParseBlock(Token endsWith, bool isFunction = false)
        {
            Scope last = scopes[scopes.Count - 1];

            Scope newScope = new Scope();
            if (isFunction)
            {
                newScope.functionId = functionIdCount++;
            }
            else
            {
                newScope.localCount = last.localCount;
                newScope.maxLocalCount = last.maxLocalCount;
                newScope.functionId = last.functionId;
            }
            scopes.Add(newScope);

            BlockTree block = new BlockTree();

            while (!accept(endsWith).valid)
            {
                Tree s = ParseStatement();

                while (accept(semicolon).valid) {}

                block.AddTree(s);

                if (s.type == TreeType.ReturnStatement)
                {
                    expect(endsWith);
                    break;
                }
            }

            scopes.RemoveAt(scopes.Count - 1);

            if (isFunction)
            {
                functionIdCount--;
            }
            else
            {
                last.maxLocalCount = Math.Max(last.maxLocalCount, newScope.maxLocalCount);
            }

            block.localCount = last.maxLocalCount;

            return block;
        }

        public Chunk ParseChunk()
        {
            scopes.Add(new Scope());

            BlockTree block = ParseBlock(Token.EOF);

            prototypes.Add(new FunctionPrototype(block.GetInstructions(), block.localCount));

            Chunk chunk = new Chunk();

            chunk.prototypes = prototypes.ToArray();

            return chunk;
        }
        
        //==========

        public Parser(string code)
        {
            lexer = new Lexer(code);

            nextToken();
        }
    }
}
