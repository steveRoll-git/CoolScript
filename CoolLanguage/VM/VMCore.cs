using System;
using System.Collections.Generic;

namespace CoolLanguage.VM
{
    enum InstructionType
    {
        /// <summary> parameter: double number. pushes the number onto the stack </summary>
        PushNumber,
        /// <summary> parameter: string. pushes the string onto the stack </summary>
        PushString,
        /// <summary> parameter: bool. pushes the boolean onto the stack </summary>
        PushBool,
        /// <summary> pushes a null value onto the stack </summary>
        PushNull,
        /// <summary>  </summary>
        Pop,

        /// <summary> parameter: string name. push the global variable onto the stack </summary>
        PushGlobal,
        /// <summary> parameter: string name. pop the last item on the stack and put it in the specified global name. </summary>
        PopGlobal,

        /// <summary> parameter: int index. push the local variable onto the stack </summary>
        GetLocal,
        /// <summary> parameter: int index. pop the last item on the stack and put it in the local variable </summary>
        SetLocal,

        /// <summary> pops index, pops table, and pushes the value on it </summary>
        GetTable,
        /// <summary> pops value, pops index, pops table, and sets the value </summary>
        SetTable,

        /// <summary> creates an empty table and pushes it onto the stack </summary>
        CreateTable,

        /// <summary> parameter: int prototype. creates a closure with the specified function prototype and pushes it onto the stack. </summary>
        CreateClosure,

        /// <summary> parameter: int amount. moves the instruction pointer by that much. (can be negative) </summary>
        Jump,
        /// <summary> paramter: int amount. pops the value at the top of the stack, and jumps by the specified amount if it's true </summary>
        JumpTrue,
        /// <summary> paramter: int amount. pops the value at the top of the stack, and jumps by the specified amount if it's false </summary>
        JumpFalse,

        /// <summary> parameter: int args. calls the last item on the stack, and uses specified number of previous items as arguments (in the order they were added) </summary>
        Call,

        /// <summary> pops the last two values and pushes their addition result </summary>
        Add,
        /// <summary> pops the last two values and pushes their subtraction result </summary>
        Sub,
        /// <summary> pops the last two values and pushes their multiplication result </summary>
        Mul,
        /// <summary> pops the last two values and pushes their division result </summary>
        Div,
        /// <summary> pops the last two values and pushes their modulo result </summary>
        Mod,
        /// <summary> pops the last two values and pushes their power result </summary>
        Pow,
        /// <summary> pops the last two values and pushes their string concat </summary>
        Concat,
        /// <summary> > </summary>
        Greater,
        /// <summary> &lt; </summary>
        Less,
        /// <summary> >= </summary>
        GEqual,
        /// <summary> &lt;= </summary>
        LEqual,
        /// <summary> == </summary>
        Equal,
        /// <summary> != </summary>
        NEqual,
        /// <summary> &amp;&amp; </summary>
        And,
        /// <summary> || </summary>
        Or,

        /// <summary> replaces last item on the stack with negation </summary>
        Negate,
        /// <summary> replaces last item on the stack with boolean not </summary>
        Not,
    }

    class VMInstruction
    {
        public InstructionType type;

        public dynamic data;

        public VMInstruction(InstructionType t, object d = null)
        {
            type = t;
            data = d;
        }
    }

    struct ExecutionStatus
    {
        public bool success;

        public string errorMessage;

        public ExecutionStatus(bool s, string message = "")
        {
            success = s;
            errorMessage = !s ? message : "";
        }
    }

    struct Chunk
    {
        /// <summary>
        /// the first prototype is what the chunk will execute
        /// </summary>
        public FunctionPrototype[] prototypes;
    }

    static class Util
    {
        public static bool isTruthyValue(ScriptValue value)
        {
            return value.type != dataType.Null && (value.type != dataType.Boolean || value.value == true);
        }
    }

    struct CFuncStatus
    {
        public bool success;
        public string errorMessage;
        public ScriptValue returnValue;

        public CFuncStatus(string errorMessage)
        {
            this.errorMessage = errorMessage;
            success = false;
            returnValue = ScriptValue.Null;
        }

        public CFuncStatus(ScriptValue returnValue)
        {
            this.returnValue = returnValue;
            success = true;
            errorMessage = "";
        }
    }

    class CoolScriptVM
    {
        static Func<ScriptValue, ScriptValue, ScriptValue>[] binaryOperators = {
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, a.value + b.value), // add
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, a.value - b.value), // sub
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, a.value * b.value), // mul
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, a.value / b.value), // div
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, a.value % b.value), // mod
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, Math.Pow(a.value, b.value)), // pow
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.String, a.value.ToString() + b.value.ToString()), // concat
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, a.value > b.value), // greater
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, a.value < b.value), // less
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, a.value >= b.value), // gequal
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, a.value <= b.value), // lequal
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, a.value == b.value), // equal
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, a.value != b.value), // nequal
            (ScriptValue a, ScriptValue b) => !Util.isTruthyValue(a) ? a : b, // and
            (ScriptValue a, ScriptValue b) => Util.isTruthyValue(a) ? a : b, // or
        };

        Stack<ScriptValue> valueStack = new Stack<ScriptValue>();

        Dictionary<string, ScriptValue> globalVars = new Dictionary<string, ScriptValue>();

        Dictionary<int, Table> tableStorage = new Dictionary<int, Table>();
        private int lastTableID = 0;

        Dictionary<int, Closure> functionStorage = new Dictionary<int, Closure>();

        Dictionary<int, Func<ScriptValue[], CFuncStatus>> CFunctionStorage = new Dictionary<int, Func<ScriptValue[], CFuncStatus>>();

        Dictionary<int, FunctionPrototype> functionPrototypes = new Dictionary<int, FunctionPrototype>();
        private int lastPrototypeID = 0;

        static CFuncStatus argError(string funcName, int argNumber, string expected, string got = "")
        {
            return new CFuncStatus(funcName + " arg #" + argNumber + ": expected " + expected + (got != "" ? (", got " + got) : ""));
        }

        static Dictionary<string, Func<ScriptValue[], CFuncStatus>> defaultFunctions = new Dictionary<string, Func<ScriptValue[], CFuncStatus>>
        {
            {"type", (ScriptValue[] args) => {
                if (args.Length <= 0)
                    return argError("type", 1, "value");
                return new CFuncStatus(new ScriptValue(dataType.String, args[0].TypeName));
            } },
            {"print", (ScriptValue[] args) => {
                Console.WriteLine(string.Join("\t", args));
                return new CFuncStatus(ScriptValue.Null);
            } },
            {"input", (ScriptValue[] args) =>
            {
                if (args.Length > 0)
                    Console.Write(args[0].value);

                return new CFuncStatus(new ScriptValue(dataType.String, Console.ReadLine()));
            } },
            {"tonumber", (ScriptValue[] args) =>
            {
                if (args.Length <= 0)
                    return argError("tonumber", 1, "value");

                if (args[0].type == dataType.Number)
                {
                     return new CFuncStatus(args[0]);
                }
                else if (args[0].type == dataType.String)
                {
                    double result;
                    if(double.TryParse(args[0].value, out result))
                    {
                        return new CFuncStatus(new ScriptValue(dataType.Number, result));
                    }
	            }
                return new CFuncStatus(ScriptValue.Null);
            } },
            {"tostring", (ScriptValue[] args) =>
            {
                if (args.Length <= 0)
                    return argError("tostring", 1, "value");

                return new CFuncStatus(new ScriptValue(dataType.String, args[0].ToString()));
            } },
        };

        private int lastCFunctionID = 0;

        static ScriptValue globalFunc_type(ScriptValue[] args)
        {
            return new ScriptValue(dataType.String, args[0].TypeName);
        }

        public CoolScriptVM()
        {
            foreach (var function in defaultFunctions)
            {
                AddCFunction(function.Value, function.Key);
            }
        }

        public ExecutionStatus Run(Closure closure)
        {
            int instructionPointer = 0;

            ScriptValue[] stackFrame = new ScriptValue[closure.prototype.stackSize];

            while(instructionPointer < closure.prototype.instructions.Length)
            {
                bool incrementIP = true;
                VMInstruction instruction = closure.prototype.instructions[instructionPointer];

                if (instruction.type == InstructionType.PushNumber)
                {
                    valueStack.Push(new ScriptValue(dataType.Number, instruction.data));
                }
                else if (instruction.type == InstructionType.PushString)
                {
                    valueStack.Push(new ScriptValue(dataType.String, instruction.data));
                }
                else if (instruction.type == InstructionType.PushBool)
                {
                    valueStack.Push(new ScriptValue(dataType.Boolean, instruction.data));
                }
                else if (instruction.type == InstructionType.PushNull)
                {
                    valueStack.Push(new ScriptValue(dataType.Null));
                }
                else if (instruction.type == InstructionType.PushGlobal)
                {
                    if (globalVars.ContainsKey(instruction.data))
                    {
                        valueStack.Push(globalVars[instruction.data]);
                    }
                    else
                    {
                        valueStack.Push(ScriptValue.Null);
                    }
                }
                else if (instruction.type == InstructionType.PopGlobal)
                {
                    ScriptValue theValue = valueStack.Pop();

                    globalVars[instruction.data] = theValue;
                }
                else if (instruction.type == InstructionType.GetLocal)
                {
                    valueStack.Push(stackFrame[instruction.data]);
                }
                else if (instruction.type == InstructionType.SetLocal)
                {
                    stackFrame[instruction.data] = valueStack.Pop();
                }
                else if (instruction.type == InstructionType.GetTable)
                {
                    ScriptValue index = valueStack.Pop();
                    ScriptValue obj = valueStack.Pop();

                    if (obj.type != dataType.Table)
                        return new ExecutionStatus(false, "Attempt to index a " + obj.TypeName + " value");

                    if (index.type == dataType.Null)
                        return new ExecutionStatus(false, "Index is null");

                    Table table = new Table();

                    if (tableStorage.TryGetValue(obj.value, out table))
                    {
                        ScriptValue value;
                        if (table.dictionary.TryGetValue(index, out value))
                        {
                            valueStack.Push(value);
                        }
                        else
                        {
                            valueStack.Push(ScriptValue.Null);
                        }
                    }
                    else
                    {
                        //normally this shouldn't happen
                        return new ExecutionStatus(false, "Table " + obj.value + " doesn't exist");
                    }
                }
                else if (instruction.type == InstructionType.SetTable)
                {
                    ScriptValue setValue = valueStack.Pop();
                    ScriptValue index = valueStack.Pop();
                    ScriptValue obj = valueStack.Pop();

                    if (obj.type != dataType.Table)
                        return new ExecutionStatus(false, "Attempt to index a " + obj.TypeName + " value");

                    if (index.type == dataType.Null)
                        return new ExecutionStatus(false, "Index is null");

                    Table table = new Table();

                    if (tableStorage.TryGetValue(obj.value, out table))
                    {
                        if (setValue.type == dataType.Null)
                            table.dictionary.Remove(index);
                        else
                            table.dictionary[index] = setValue;
                    }
                    else
                    {
                        //normally this shouldn't happen
                        return new ExecutionStatus(false, "Table " + obj.value + " doesn't exist");
                    }
                }
                else if (instruction.type == InstructionType.Call)
                {
                    ScriptValue function = valueStack.Pop();

                    if (function.type == dataType.Function)
                    {

                    }
                    else if (function.type == dataType.CFunction)
                    {
                        Func<ScriptValue[], CFuncStatus> cFunc;
                        if (!CFunctionStorage.TryGetValue(function.value, out cFunc))
                        {
                            //normally this shouldn't happen
                            return new ExecutionStatus(false, "CFunction " + function.value + " doesn't exist");
                        }

                        int argCount = instruction.data;

                        ScriptValue[] arguments = new ScriptValue[argCount];

                        for (int a = argCount - 1; a >= 0; a--)
                        {
                            arguments[a] = valueStack.Pop();
                        }

                        CFuncStatus status = cFunc(arguments);

                        if (!status.success)
                        {
                            return new ExecutionStatus(false, status.errorMessage);
                        }

                        valueStack.Push(status.returnValue);
                    }
                    else
                    {
                        return new ExecutionStatus(false, "Attempt to call a " + function.TypeName + " value");
                    }
                }
                else if (instruction.type == InstructionType.CreateTable)
                {
                    Table table = new Table();
                    int id = lastTableID++;
                    tableStorage.Add(id, table);
                    valueStack.Push(new ScriptValue(dataType.Table, id));
                }
                else if (instruction.type >= InstructionType.Add && instruction.type <= InstructionType.Or)
                {
                    ScriptValue value2 = valueStack.Pop();
                    ScriptValue value1 = valueStack.Pop();

                    if(instruction.type == InstructionType.Concat)
                    {
                        if (value1.type != dataType.String && value1.type != dataType.Number)
                        {
                            return new ExecutionStatus(false, "Attempt to concatenate a " + value1.TypeName + " value");
                        }
                        if (value2.type != dataType.String && value2.type != dataType.Number)
                        {
                            return new ExecutionStatus(false, "Attempt to concatenate a " + value2.TypeName + " value");
                        }
                    }
                    else if (instruction.type >= InstructionType.Add && instruction.type <= InstructionType.Pow)
                    {
                        if (value1.type != dataType.Number)
                        {
                            return new ExecutionStatus(false, "Attempt to perform arithmetic on " + value1.TypeName + " value");
                        }
                        if (value2.type != dataType.Number)
                        {
                            return new ExecutionStatus(false, "Attempt to perform arithmetic on " + value2.TypeName + " value");
                        }
                    }
                    else if (instruction.type >= InstructionType.Greater && instruction.type <= InstructionType.LEqual)
                    {
                        if (value1.type != dataType.Number || value2.type != dataType.Number)
                        {
                            return new ExecutionStatus(false, "Attempt to compare " + value1.TypeName + " with " + value2.TypeName);
                        }
                    }

                    valueStack.Push(binaryOperators[instruction.type - InstructionType.Add](value1, value2));
                }
                else if (instruction.type == InstructionType.Jump || instruction.type == InstructionType.JumpTrue || instruction.type == InstructionType.JumpFalse)
                {
                    if (instruction.type == InstructionType.JumpTrue || instruction.type == InstructionType.JumpFalse)
                    {
                        ScriptValue value = valueStack.Pop();
                        if (Util.isTruthyValue(value) ^ instruction.type == InstructionType.JumpTrue)
                        {
                            goto dont;
                        }
                    }
                    instructionPointer += instruction.data;
                    incrementIP = false;
                dont:;
                }
                else if (instruction.type == InstructionType.Negate)
                {
                    ScriptValue value = valueStack.Pop();

                    if (value.type != dataType.Number)
                    {
                        return new ExecutionStatus(false, "Attempt to negate a " + value.TypeName + " value");
                    }

                    valueStack.Push(new ScriptValue(dataType.Number, -value.value));
                }

                if (incrementIP)
                {
                    instructionPointer++;
                }
            }

            return new ExecutionStatus(true);
        }

        public ExecutionStatus ExecuteChunk(Chunk chunk)
        {
            int firstId = lastPrototypeID;
            foreach (FunctionPrototype prototype in chunk.prototypes)
            {
                functionPrototypes.Add(lastPrototypeID++, prototype);
            }

            return Run(new Closure(functionPrototypes[firstId]));
        }

        public ScriptValue getStackLast()
        {
            return valueStack.Peek();
        }

        public void AddCFunction(Func<ScriptValue[], CFuncStatus> function, string globalName)
        {
            int id = lastCFunctionID++;
            CFunctionStorage.Add(id, function);
            globalVars.Add(globalName, new ScriptValue(dataType.CFunction, id));
        }

        public void ClearStack()
        {
            valueStack.Clear();
        }
    }
}
