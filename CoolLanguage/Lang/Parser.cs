using System;
using System.Collections.Generic;
using CoolScript.Lang;
using CoolScript.VM;

namespace CoolScript.Lang
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
        CreateArray,
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
            if (value != null)
            {
                VMInstruction[] valueInstructions = value.GetInstructions();

                VMInstruction[] toReturn = new VMInstruction[valueInstructions.Length + 1];

                Array.Copy(valueInstructions, toReturn, valueInstructions.Length);

                toReturn[toReturn.Length - 1] = new VMInstruction(InstructionType.PopGlobal, varName);

                return toReturn;
            }
            
            return new VMInstruction[] { };
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
            value = _value ?? new NullTree();
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

            toReturn[toReturn.Length - 1] = new VMInstruction(InstructionType.GetIndex);

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

            toReturn[toReturn.Length - 1] = new VMInstruction(InstructionType.SetIndex, false);

            return toReturn;
        }
    }

    class FunctionCallTree : Tree
    {
        private List<Tree> arguments = new List<Tree>();

        public Tree functionTree;

        public bool isExpression = false;

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

            if (isExpression)
            {
                instructions.Add(new VMInstruction(InstructionType.PushReturn));
            }

            return instructions.ToArray();
        }
    }

    class CreateTableTree : Tree
    {
        private List<TableKeyValue> values = new List<TableKeyValue>();

        public CreateTableTree()
        {
            type = TreeType.CreateTable;
        }

        public void AddValue(TableKeyValue value)
        {
            values.Add(value);
        }

        public override VMInstruction[] GetInstructions()
        {
            List<VMInstruction> instructions = new List<VMInstruction>();

            instructions.Add(new VMInstruction(InstructionType.CreateTable));

            for (int i = 0; i < values.Count; i++)
            {
                instructions.Add(new VMInstruction(InstructionType.PushString, values[i].key));
                instructions.AddRange(values[i].value.GetInstructions());
                instructions.Add(new VMInstruction(InstructionType.SetIndex, true));
            }

            return instructions.ToArray();
        }
    }

    class CreateArrayTree : Tree
    {
        private List<Tree> items = new List<Tree>();

        public CreateArrayTree()
        {
            type = TreeType.CreateArray;
        }

        public void AddItem(Tree item)
        {
            items.Add(item);
        }

        public override VMInstruction[] GetInstructions()
        {
            List<VMInstruction> toReturn = new List<VMInstruction>();

            foreach (Tree item in items)
            {
                toReturn.AddRange(item.GetInstructions());
            }

            toReturn.Add(new VMInstruction(InstructionType.CreateArray, items.Count));

            return toReturn.ToArray();
        }
    }

    class CreateClosureTree : Tree
    {
        public int prototypeId;

        public CreateClosureTree(int prototypeId)
        {
            this.prototypeId = prototypeId;
        }

        public override VMInstruction[] GetInstructions()
        {
            return new VMInstruction[] { new VMInstruction(InstructionType.CreateClosure, prototypeId) };
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

    class IfTree : Tree
    {
        public Tree condition;
        public Tree ifBody;

        public bool hasElse;
        public Tree elseBody;

        public IfTree()
        {

        }

        public override VMInstruction[] GetInstructions()
        {
            List<VMInstruction> toReturn = new List<VMInstruction>();

            toReturn.AddRange(condition.GetInstructions());

            VMInstruction[] bodyInstructions = ifBody.GetInstructions();

            toReturn.Add(new VMInstruction(InstructionType.JumpFalse, bodyInstructions.Length + (hasElse ? 2 : 1)));
            toReturn.AddRange(bodyInstructions);

            if (hasElse)
            {
                VMInstruction[] elseInstructions = elseBody.GetInstructions();

                toReturn.Add(new VMInstruction(InstructionType.Jump, elseInstructions.Length + 1));

                toReturn.AddRange(elseInstructions);
            }

            return toReturn.ToArray();
        }
    }

    class WhileTree : Tree
    {
        public Tree condition;
        public Tree body;

        public WhileTree()
        {

        }

        public override VMInstruction[] GetInstructions()
        {
            List<VMInstruction> toReturn = new List<VMInstruction>();

            VMInstruction[] conditionInstructions = condition.GetInstructions();
            VMInstruction[] bodyInstructions = body.GetInstructions();

            toReturn.AddRange(conditionInstructions);
            toReturn.Add(new VMInstruction(InstructionType.JumpFalse, bodyInstructions.Length + 2));
            toReturn.AddRange(bodyInstructions);
            toReturn.Add(new VMInstruction(InstructionType.Jump, -bodyInstructions.Length - conditionInstructions.Length - 1));

            return toReturn.ToArray();
        }
    }

    class ReturnTree : Tree
    {
        public Tree value;

        public ReturnTree(Tree value = null)
        {
            type = TreeType.ReturnStatement;

            this.value = value;
        }

        public override VMInstruction[] GetInstructions()
        {
            if (value != null)
            {
                List<VMInstruction> toReturn = new List<VMInstruction>();
                toReturn.AddRange(value.GetInstructions());
                toReturn.Add(new VMInstruction(InstructionType.Return, true));
                return toReturn.ToArray();
            }
            else
            {
                return new VMInstruction[] { new VMInstruction(InstructionType.Return, false) };
            }
            
        }
    }

    enum ScopeType
    {
        None,
        Chunk,
        If,
        While,
        Function
    }

    class Scope
    {
        public Dictionary<string, int> localVariables = new Dictionary<string, int>();
        public Dictionary<string, bool> globalVariables = new Dictionary<string, bool>();

        public int localCount = 0;
        public int maxLocalCount = 0;

        public int functionId;

        public ScopeType type;

        //public FunctionPrototype prototype;

        public Scope(ScopeType type, int _localCount = 0)
        {
            this.type = type;
            localCount = _localCount;
        }
    }

    struct TableKeyValue
    {
        public string key;
        public Tree value;

        public TableKeyValue(string key, Tree value)
        {
            this.key = key;
            this.value = value;
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
        static Token colon = new Token(TokenType.Punctuation, ":");

        static Token kTrue = new Token(TokenType.Keyword, "true");
        static Token kFalse = new Token(TokenType.Keyword, "false");
        static Token kNull = new Token(TokenType.Keyword, "null");

        static Token kFunction = new Token(TokenType.Keyword, "function");

        static Token kElse = new Token(TokenType.Keyword, "else");

        private List<Scope> scopes = new List<Scope>();

        private int functionIdCount = 0;
        private int prototypeOffset = 0;

        private List<FunctionPrototype> prototypes = new List<FunctionPrototype>();

        //////////
        
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
                else if (scopes[i].globalVariables.ContainsKey(name))
                {
                    return new GlobalValueTree(name);
                }
            }

            return null;
        }

        //==========

        private Tree ParseIndexOrCall(Tree theObject = null)
        {
            if (theObject == null)
            {
                if (curToken.type == TokenType.Identifier)
                {
                    theObject = GetVariableFromName(curToken.value);

                    if (theObject == null)
                    {
                        throw new ReferenceErrorException(curToken.line, $"'{curToken.value}' is not defined");
                    }

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

            if (theObject.type == TreeType.FunctionCall)
                (theObject as FunctionCallTree).isExpression = true;

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

            if (theObject.type == TreeType.FunctionCall)
                (theObject as FunctionCallTree).isExpression = false;

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
                CreateTableTree tree = new CreateTableTree();
                if (!accept(rCurly).valid)
                {
                    do
                    {
                        Token keyName = accept(Token.String);
                        if (!keyName.valid)
                            keyName = accept(Token.Identifier);

                        if (!keyName.valid)
                            throw new SyntaxErrorException(curToken.line, "table key must be identifier or string (was " + curToken.type + ")");

                        expect(colon);

                        Tree value = ParseExpression();

                        tree.AddValue(new TableKeyValue(keyName.value, value));
                    } while (accept(comma).valid);
                    expect(rCurly);
                }
                return tree;
            }

            Token square = accept(lSquare);
            if (square.valid)
            {
                CreateArrayTree tree = new CreateArrayTree();
                if (!accept(rSquare).valid)
                {
                    do
                    {
                        tree.AddItem(ParseExpression());
                    } while (accept(comma).valid);
                    expect(rSquare);
                }
                return tree;
            }

            if (accept(kFunction).valid)
            {
                return ParseFunctionDeclaration();
            }

            /*expect(lParen);
            Tree exp = ParseExpression();
            expect(rParen);*/

            Tree obj = ParseIndexOrCall();

            if (obj.type == TreeType.FunctionCall)
            {
                (obj as FunctionCallTree).isExpression = true;
            }

            return obj;
        }

        private Tree ParseExpression1(Tree lhs, int minPrecedence) // https://en.wikipedia.org/wiki/Operator-precedence_parser literally just copied the pseudocode
        {
            while( curToken.type == TokenType.BinaryOperator && curToken.precedence >= minPrecedence){

                Token op = curToken;
                nextToken();
                Tree rhs = ParsePrimary();
                while(curToken.type == TokenType.BinaryOperator && ((curToken.precedence > op.precedence) || (curToken.associativity == Associativity.Right && curToken.precedence == op.precedence)))
                {
                    rhs = ParseExpression1(rhs, curToken.precedence);
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

                if (keyword.value == "var" || keyword.value == "global")
                {
                    bool isLocal = keyword.value == "var";

                    string varName = curToken.value;

                    nextToken();

                    Tree expression = null;

                    if (accept(equals).valid)
                    {
                        expression = ParseExpression();
                    }

                    Scope last = scopes[scopes.Count - 1];

                    if (isLocal)
                    {
                        last.localVariables.Add(varName, last.localCount);

                        last.localCount++;
                        last.maxLocalCount = Math.Max(last.localCount, last.maxLocalCount);

                        return new LocalAssignmentTree(last.localCount - 1, expression);
                    }
                    else
                    {
                        last.globalVariables.Add(varName, true);

                        return new GlobalAssignmentTree(varName, expression);
                    }
                }
                else if (keyword.value == "function")
                {
                    string name = expect(Token.Identifier).value;

                    CreateClosureTree funcTree = ParseFunctionDeclaration();

                    GlobalAssignmentTree tree = new GlobalAssignmentTree(name, funcTree);

                    return tree;
                }
                else if (keyword.value == "if")
                {
                    IfTree tree = new IfTree();

                    expect(lParen);

                    tree.condition = ParseExpression();

                    expect(rParen);

                    tree.ifBody = ParseBlockOrStatement(ScopeType.If);

                    if (accept(kElse).valid)
                    {
                        tree.hasElse = true;
                        tree.elseBody = ParseBlockOrStatement(ScopeType.If);
                    }

                    return tree;
                }
                else if (keyword.value == "while")
                {
                    WhileTree tree = new WhileTree();

                    expect(lParen);

                    tree.condition = ParseExpression();

                    expect(rParen);

                    tree.body = ParseBlockOrStatement(ScopeType.While);

                    return tree;
                }
                else if (keyword.value == "return")
                {
                    ReturnTree tree = new ReturnTree();

                    if (curToken.type != TokenType.Punctuation)
                    {
                        tree.value = ParseExpression();
                    }

                    return tree;
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

        private BlockTree ParseBlock(ScopeType type, Token endsWith, IEnumerable<string> globalVars = null, bool isFunction = false, string[] localVars = null)
        {
            Scope last = null;
            bool hasLast = scopes.Count > 0;
            if (hasLast)
                last = scopes[scopes.Count - 1];

            Scope newScope = new Scope(type);

            if (isFunction)
            {
                newScope.functionId = ++functionIdCount;
                for(int i = 0; i < localVars.Length; i++)
                {
                    newScope.localVariables.Add(localVars[i], i);
                }
                newScope.localCount = localVars.Length;
                newScope.maxLocalCount = localVars.Length;
            }
            else if (hasLast)
            {
                newScope.localCount = last.localCount;
                newScope.maxLocalCount = last.maxLocalCount;
                newScope.functionId = last.functionId;
            }

            if (globalVars != null)
            {
                foreach (string name in globalVars)
                {
                    newScope.globalVariables.Add(name, true);
                }
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
            else if(hasLast)
            {
                last.maxLocalCount = Math.Max(last.maxLocalCount, newScope.maxLocalCount);
            }

            if (hasLast && !isFunction)
                block.localCount = last.maxLocalCount;
            else
                block.localCount = newScope.maxLocalCount;

            return block;
        }

        public Tree ParseBlockOrStatement(ScopeType type)
        {
            if (accept(lCurly).valid)
            {
                return ParseBlock(type, rCurly);
            }
            else
            {
                return ParseStatement();
            }
        }

        private CreateClosureTree ParseFunctionDeclaration() // starts with argument lParen
        {
            List<string> arguments = new List<string>();

            expect(lParen);

            if (!accept(rParen).valid)
            {
                do
                {
                    arguments.Add(expect(Token.Identifier).value);
                } while (accept(comma).valid);
                expect(rParen);
            }

            expect(lCurly);

            BlockTree block = ParseBlock(ScopeType.Function, rCurly, null, true, arguments.ToArray());

            prototypes.Add(new FunctionPrototype(block.GetInstructions(), block.localCount, arguments.Count));

            return new CreateClosureTree(prototypes.Count - 1 + prototypeOffset);
        }

        public Chunk ParseChunk(int prototypeOffset, IEnumerable<string> knownGlobals)
        {
            this.prototypeOffset = prototypeOffset;

            BlockTree block = ParseBlock(ScopeType.Chunk, Token.EOF, knownGlobals);

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
