using System;
using System.Collections.Generic;
using CoolScript.Lang;

namespace CoolScript.VM
{
    class ScriptRuntimeException : Exception
    {
        public ScriptRuntimeException()
        {

        }

        public ScriptRuntimeException(string details)
            : base(String.Format("Runtime error: {0}", details))
        {

        }
    }

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

        /// <summary> pops index, pops object, and pushes the value on it </summary>
        GetIndex,
        /// <summary> parameter: bool keep. pops value, pops index, and if keep is false - pops object, and sets the value. </summary>
        SetIndex,

        /// <summary> creates an empty table and pushes it onto the stack </summary>
        CreateTable,
        /// <summary> parameter: int args. creates an array and pushes it onto the stack. if arg is greater than 0, moves<br/>that many elements from the stack to the array (in the order they were added) </summary>
        CreateArray,

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
        /// <summary> parameter: bool hasValue. returns from the current function, and optionally sets the return register to the popped stack value </summary>
        Return,
        /// <summary> pushes the value of the return register onto the stack </summary>
        PushReturn,

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

        public object data;

        public VMInstruction(InstructionType t, object d = null)
        {
            type = t;
            data = d;
        }

        public override string ToString()
        {
            return type + " " + data;
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
        /// the last prototype is what the chunk will execute
        /// </summary>
        public FunctionPrototype[] prototypes;
    }

    static class Util
    {
        public static bool isTruthyValue(ScriptValue value)
        {
            return value.type != dataType.Null && (value.type != dataType.Boolean || (bool)value.value == true);
        }

        public static bool isWhole(double d)
        {
            return Math.Abs(d % 1) <= (Double.Epsilon * 100);
        }

        public static bool AreValuesEqual(ScriptValue a, ScriptValue b)
        {
            return a.type == b.type && a.value.Equals(b.value);
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

    class ClosureInstance
    {
        public Closure closure;
        public int instructionPointer = 0;
        public ScriptValue[] stackFrame;

        public ClosureInstance(Closure closure)
        {
            this.closure = closure;

            stackFrame = new ScriptValue[closure.prototype.stackSize];
        }
    }

    class CoolScriptVM
    {
        static Func<ScriptValue, ScriptValue, ScriptValue>[] binaryOperators = {
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, (double)a.value + (double)b.value), // add
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, (double)a.value - (double)b.value), // sub
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, (double)a.value * (double)b.value), // mul
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, (double)a.value / (double)b.value), // div
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, (double)a.value % (double)b.value), // mod
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Number, Math.Pow((double)a.value, (double)b.value)), // pow
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.String, a.value.ToString() + b.value.ToString()), // concat
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, (double)a.value > (double)b.value), // greater
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, (double)a.value < (double)b.value), // less
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, (double)a.value >= (double)b.value), // gequal
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, (double)a.value <= (double)b.value), // lequal
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, Util.AreValuesEqual(a, b)), // equal
            (ScriptValue a, ScriptValue b) => new ScriptValue(dataType.Boolean, !Util.AreValuesEqual(a, b)), // nequal
            (ScriptValue a, ScriptValue b) => !Util.isTruthyValue(a) ? a : b, // and
            (ScriptValue a, ScriptValue b) => Util.isTruthyValue(a) ? a : b, // or
        };

        private List<ClosureInstance> callStack = new List<ClosureInstance>();
        private const int maxStackDepth = 100;

        private Stack<ScriptValue> valueStack = new Stack<ScriptValue>();

        private Dictionary<string, ScriptValue> globalVars = new Dictionary<string, ScriptValue>();

        private ScriptValue returnRegister;

        private Dictionary<int, Table> tableStorage = new Dictionary<int, Table>();
        private int lastTableID = 0;

        private Dictionary<int, ScriptArray> arrayStorage = new Dictionary<int, ScriptArray>();
        private int lastArrayID = 0;

        private Dictionary<int, Closure> functionStorage = new Dictionary<int, Closure>();
        private int lastFunctionID = 0;

        private Dictionary<int, Func<ScriptValue[], CFuncStatus>> CFunctionStorage = new Dictionary<int, Func<ScriptValue[], CFuncStatus>>();
        private int lastCFunctionID = 0;

        private Dictionary<int, FunctionPrototype> functionPrototypes = new Dictionary<int, FunctionPrototype>();
        public int lastPrototypeID
        {
            get;
            private set;
        } = 0;

        private Random randomGenerator = new Random();

        static CFuncStatus argError(string funcName, int argNumber, string expected, string got = "")
        {
            return new CFuncStatus(funcName + " arg #" + argNumber + ": expected " + expected + (got != "" ? (", got " + got) : ""));
        }

        public CoolScriptVM()
        {
            Dictionary<string, Func<ScriptValue[], CFuncStatus>> defaultFunctions = new Dictionary<string, Func<ScriptValue[], CFuncStatus>>
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
                        if(double.TryParse((string)args[0].value, out result))
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
                {"length", (ScriptValue[] args) =>
                {
                    if (args.Length <= 0)
                        return argError("length", 1, "value");

                    if(args[0].type == dataType.String)
                    {
                        return new CFuncStatus(new ScriptValue(dataType.Number, ((string)args[0].value).Length));
                    }
                    else if(args[0].type == dataType.Array)
                    {
                        if(arrayStorage.TryGetValue((int)args[0].value, out ScriptArray array))
                        {
                            return new CFuncStatus(new ScriptValue(dataType.Number, (double)array.list.Count));
                        }
                        else
                        {
                            //normally this shouldn't happen
                            return new CFuncStatus("Array " + args[0].value + " doesn't exist");
                        }
                    }
                    else
                    {
                        return argError("length", 1, "string or array", args[0].TypeName);
                    }
                } },
                {"random", (ScriptValue[] args) =>
                {
                    if (args.Length == 0)
                    {
                        return new CFuncStatus(new ScriptValue(dataType.Number, randomGenerator.NextDouble()));
                    }
                    else if (args.Length == 1)
                    {
                        if(args[0].type != dataType.Number)
                            return argError("random", 1, "number", args[0].TypeName);
                        return new CFuncStatus(new ScriptValue(dataType.Number, Math.Floor(randomGenerator.NextDouble() * (double)args[0].value)));
                    }
                    else
                    {
                        if(args[0].type != dataType.Number)
                            return argError("random", 1, "number", args[0].TypeName);
                        if(args[1].type != dataType.Number)
                            return argError("random", 2, "number", args[1].TypeName);
                        return new CFuncStatus(new ScriptValue(dataType.Number, Math.Floor((double)args[0].value + randomGenerator.NextDouble() * ((double)args[1].value - (double)args[0].value))));
                    }
                } },
                {"randomseed", (ScriptValue[] args) =>
                {
                    if (args.Length <= 0)
                        return argError("randomseed", 1, "value");

                    if(args[0].type != dataType.Number)
                        return argError("randomseed", 1, "number", args[0].TypeName);

                    randomGenerator = new Random((int)(double)args[0].value);

                    return new CFuncStatus(ScriptValue.Null);
                } },
                {"loadstring", (ScriptValue[] args) =>
                {
                    if (args.Length <= 0)
                        return argError("loadstring", 1, "value");

                    if(args[0].type != dataType.String)
                        return argError("loadstring", 1, "string", args[0].TypeName);

                    Closure newClosure = LoadChunk((string)args[0].value);
                    ScriptValue function = AddClosure(newClosure);
                    return new CFuncStatus(function);
                } },
                {"loadfile", (ScriptValue[] args) =>
                {
                    if (args.Length <= 0)
                        return argError("loadfile", 1, "value");

                    if(args[0].type != dataType.String)
                        return argError("loadstring", 1, "string", args[0].TypeName);

                    string content;

                    try
                    {
                        content = System.IO.File.ReadAllText((string)args[0].value);
                    }
                    catch (Exception e)
                    {
                        return new CFuncStatus(e.Message);
                    }

                    Closure newClosure = LoadChunk(content);
                    ScriptValue function = AddClosure(newClosure);
                    return new CFuncStatus(function);
                } }
            };
            foreach (var function in defaultFunctions)
            {
                AddCFunction(function.Value, function.Key);
            }
        }

        public ExecutionStatus Run(Closure closure, ScriptValue[] arguments)
        {
            if (callStack.Count >= maxStackDepth)
            {
                callStack.Clear();
                return new ExecutionStatus(false, "stack overflow");
            }

            ClosureInstance instance = new ClosureInstance(closure);

            callStack.Add(instance);

            for (int i = 0; i < arguments.Length; i++)
            {
                instance.stackFrame[i] = arguments[i];
            }

            while(instance.instructionPointer < closure.prototype.instructions.Length)
            {
                bool incrementIP = true;
                VMInstruction instruction = closure.prototype.instructions[instance.instructionPointer];

                if (instruction.type == InstructionType.PushNumber)
                {
                    PushNumber((double)instruction.data);
                }
                else if (instruction.type == InstructionType.PushString)
                {
                    PushString((string)instruction.data);
                }
                else if (instruction.type == InstructionType.PushBool)
                {
                    PushBool((bool)instruction.data);
                }
                else if (instruction.type == InstructionType.PushNull)
                {
                    PushNull();
                }
                else if (instruction.type == InstructionType.PushGlobal)
                {
                    PushGlobal((string)instruction.data);
                }
                else if (instruction.type == InstructionType.PopGlobal)
                {
                    PopGlobal((string)instruction.data);
                }
                else if (instruction.type == InstructionType.GetLocal)
                {
                    valueStack.Push(instance.stackFrame[(int)instruction.data]);
                }
                else if (instruction.type == InstructionType.SetLocal)
                {
                    instance.stackFrame[(int)instruction.data] = valueStack.Pop();

                    GC_FullCycle();
                }
                else if (instruction.type == InstructionType.GetIndex)
                {
                    ExecutionStatus status = GetIndex();

                    if (!status.success)
                    {
                        return status;
                    }
                }
                else if (instruction.type == InstructionType.SetIndex)
                {
                    ExecutionStatus status = SetIndex((bool)instruction.data);

                    if (!status.success)
                    {
                        return status;
                    }
                }
                else if (instruction.type == InstructionType.Call)
                {
                    ExecutionStatus status = Call((int)instruction.data);

                    if (!status.success)
                    {
                        return status;
                    }
                }
                else if (instruction.type == InstructionType.CreateTable)
                {
                    CreateTable();
                }
                else if (instruction.type == InstructionType.CreateArray)
                {
                    CreateArray((int)instruction.data);
                }
                else if (instruction.type == InstructionType.CreateClosure)
                {
                    if (functionPrototypes.TryGetValue((int)instruction.data, out FunctionPrototype prototype))
                    {
                        Closure newClosure = new Closure(prototype);
                        ScriptValue function = AddClosure(newClosure);

                        valueStack.Push(function);
                    }
                    else
                    {
                        //normally this shouldn't happen
                        return new ExecutionStatus(false, "Function prototype " + instruction.data + " doesn't exist");
                    }
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
                    instance.instructionPointer += (int)instruction.data;
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

                    valueStack.Push(new ScriptValue(dataType.Number, -(double)value.value));
                }
                else if (instruction.type == InstructionType.Return)
                {
                    if ((bool)instruction.data)
                    {
                        returnRegister = valueStack.Pop();
                    }
                    return new ExecutionStatus(true);
                }
                else if (instruction.type == InstructionType.PushReturn)
                {
                    valueStack.Push(returnRegister);
                }

                if (incrementIP)
                {
                    instance.instructionPointer++;
                }
            }

            returnRegister = ScriptValue.Null;

            callStack.RemoveAt(callStack.Count - 1);

            return new ExecutionStatus(true);
        }

        public void PushNumber(double num)
        {
            valueStack.Push(new ScriptValue(dataType.Number, num));
        }

        public void PushString(string str)
        {
            valueStack.Push(new ScriptValue(dataType.String, str));
        }

        public void PushBool(bool val)
        {
            valueStack.Push(new ScriptValue(dataType.Boolean, val));
        }

        public void PushNull()
        {
            valueStack.Push(ScriptValue.Null);
        }

        public void PushGlobal(string name)
        {
            if (globalVars.ContainsKey(name))
            {
                valueStack.Push(globalVars[name]);
            }
            else
            {
                valueStack.Push(ScriptValue.Null);
            }
        }

        public void PopGlobal(string name)
        {
            ScriptValue theValue = valueStack.Pop();

            globalVars[name] = theValue;

            GC_FullCycle();
        }

        public ExecutionStatus GetIndex()
        {
            ScriptValue index = valueStack.Pop();
            ScriptValue obj = valueStack.Pop();

            if (obj.type == dataType.Table)
            {

                if (tableStorage.TryGetValue((int)obj.value, out Table table))
                {
                    if (index.type != dataType.String)
                    {
                        return new ExecutionStatus(false, "Attempt to index table with " + index.TypeName + " value");
                    }
                    else
                    {
                        if (table.dictionary.TryGetValue((string)index.value, out ScriptValue value))
                        {
                            valueStack.Push(value);
                        }
                        else
                        {
                            valueStack.Push(ScriptValue.Null);
                        }
                    }
                }
                else
                {
                    //normally this shouldn't happen
                    return new ExecutionStatus(false, "Table " + obj.value + " doesn't exist");
                }
            }
            else if (obj.type == dataType.Array)
            {

                if (arrayStorage.TryGetValue((int)obj.value, out ScriptArray array))
                {
                    if (index.type != dataType.Number)
                    {
                        return new ExecutionStatus(false, "Attempt to index array with " + index.TypeName + " value");
                    }
                    else if (!Util.isWhole((double)index.value) || (double)index.value < 0D)
                    {
                        return new ExecutionStatus(false, "Array index must be whole and positive");
                    }
                    else
                    {
                        int actualIndex = (int)(double)index.value;

                        if (actualIndex < array.list.Count)
                        {
                            valueStack.Push(array.list[actualIndex]);
                        }
                        else
                        {
                            valueStack.Push(ScriptValue.Null);
                        }
                    }
                }
                else
                {
                    //normally this shouldn't happen
                    return new ExecutionStatus(false, "Array " + obj.value + " doesn't exist");
                }
            }
            else
            {
                return new ExecutionStatus(false, "Attempt to index a " + obj.TypeName + " value");
            }

            return new ExecutionStatus(true);
        }

        public ExecutionStatus SetIndex(bool keepTable)
        {
            ScriptValue setValue = valueStack.Pop();
            ScriptValue index = valueStack.Pop();
            ScriptValue obj = keepTable ? valueStack.Peek() : valueStack.Pop();

            if (obj.type == dataType.Table)
            {

                if (tableStorage.TryGetValue((int)obj.value, out Table table))
                {
                    if (index.type != dataType.String)
                    {
                        return new ExecutionStatus(false, "Attempt to index table with " + index.TypeName + " value");
                    }
                    else
                    {
                        table.dictionary[(string)index.value] = setValue;
                    }
                }
                else
                {
                    //normally this shouldn't happen
                    return new ExecutionStatus(false, "Table " + obj.value + " doesn't exist");
                }
            }
            else if (obj.type == dataType.Array)
            {

                if (arrayStorage.TryGetValue((int)obj.value, out ScriptArray array))
                {
                    if (index.type != dataType.Number)
                    {
                        return new ExecutionStatus(false, "Attempt to index array with " + index.TypeName + " value");
                    }
                    else if (!Util.isWhole((double)index.value) || (double)index.value < 0D)
                    {
                        return new ExecutionStatus(false, "Array index must be whole and positive");
                    }
                    else
                    {
                        int actualIndex = (int)(double)index.value;

                        if (actualIndex >= array.list.Count)
                        {
                            while (array.list.Count <= actualIndex)
                            {
                                array.list.Add(ScriptValue.Null);
                            }
                        }

                        array.list[actualIndex] = setValue;
                    }
                }
                else
                {
                    //normally this shouldn't happen
                    return new ExecutionStatus(false, "Array " + obj.value + " doesn't exist");
                }
            }
            else
            {
                return new ExecutionStatus(false, "Attempt to index a " + obj.TypeName + " value");
            }

            return new ExecutionStatus(true);
        }

        public ExecutionStatus Call(int argCount)
        {
            ScriptValue function = valueStack.Pop();

            if (function.type == dataType.Function)
            {
                if (functionStorage.TryGetValue((int)function.value, out Closure theClosure))
                {
                    ScriptValue[] args = new ScriptValue[theClosure.prototype.argCount];

                    for (int a = argCount - 1; a >= 0; a--)
                    {
                        ScriptValue value = valueStack.Pop();
                        if (a < theClosure.prototype.argCount)
                            args[a] = value;
                    }

                    ExecutionStatus status = Run(theClosure, args);
                    if (!status.success)
                    {
                        return status;
                    }
                }
                else
                {
                    //normally this shouldn't happen
                    return new ExecutionStatus(false, "Function " + function.value + " doesn't exist");
                }
            }
            else if (function.type == dataType.CFunction)
            {
                Func<ScriptValue[], CFuncStatus> cFunc;
                if (!CFunctionStorage.TryGetValue((int)function.value, out cFunc))
                {
                    //normally this shouldn't happen
                    return new ExecutionStatus(false, "CFunction " + function.value + " doesn't exist");
                }

                ScriptValue[] args = new ScriptValue[argCount];

                for (int a = argCount - 1; a >= 0; a--)
                {
                    args[a] = valueStack.Pop();
                }

                CFuncStatus status = cFunc(args);

                if (!status.success)
                {
                    return new ExecutionStatus(false, status.errorMessage);
                }

                returnRegister = status.returnValue;
            }
            else
            {
                return new ExecutionStatus(false, "Attempt to call a " + function.TypeName + " value");
            }

            GC_FullCycle();

            return new ExecutionStatus(true);
        }

        public void CreateTable()
        {
            Table table = new Table();
            table.mark = lastGCMark;
            int id = lastTableID++;
            tableStorage.Add(id, table);
            valueStack.Push(new ScriptValue(dataType.Table, id));
        }

        public void CreateArray(int numItems)
        {
            ScriptArray array = new ScriptArray();
            array.mark = lastGCMark;
            int id = lastArrayID++;
            arrayStorage.Add(id, array);

            for (int i = 0; i < numItems; i++)
            {
                array.list.Insert(0, valueStack.Pop());
            }

            valueStack.Push(new ScriptValue(dataType.Array, id));
        }

        public Closure LoadChunk(string code)
        {
            Parser parser = new Parser(code);

            Chunk chunk = parser.ParseChunk(lastPrototypeID);

            foreach (FunctionPrototype prototype in chunk.prototypes)
            {
                functionPrototypes.Add(lastPrototypeID++, prototype);
            }

            return new Closure(functionPrototypes[functionPrototypes.Count - 1]);
        }

        private ScriptValue AddClosure(Closure closure)
        {
            int id = lastFunctionID++;
            functionStorage.Add(id, closure);
            return new ScriptValue(dataType.Function, id);
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

        byte lastGCMark = 0;

        private void GC_MarkValue(ScriptValue value)
        {
            if (value.type == dataType.Array)
            {
                if (arrayStorage.TryGetValue((int)value.value, out ScriptArray array))
                {
                    if (array.mark != lastGCMark)
                    {
                        array.mark = lastGCMark;
                        GC_MarkCollection(array.list);
                    }
                }
                else
                {
                    throw new ScriptRuntimeException("array " + value.value + " doesn't exist");
                }
            }
            else if (value.type == dataType.Table)
            {
                if (tableStorage.TryGetValue((int)value.value, out Table table))
                {
                    if (table.mark != lastGCMark)
                    {
                        table.mark = lastGCMark;
                        GC_MarkCollection(table.dictionary.Values);
                    }
                }
                else
                {
                    throw new ScriptRuntimeException("table " + value.value + " doesn't exist");
                }
            }
            else if (value.type == dataType.Function)
            {
                if (functionStorage.TryGetValue((int)value.value, out Closure closure))
                {
                    if (closure.mark != lastGCMark)
                    {
                        closure.mark = lastGCMark;
                    }
                }
                else
                {
                    throw new ScriptRuntimeException("function " + value.value + " doesn't exist");
                }
            }
        }

        private void GC_MarkCollection(IEnumerable<ScriptValue> root)
        {
            foreach (ScriptValue value in root)
            {
                GC_MarkValue(value);
            }
        }

        private void GC_Sweep()
        {
            List<int> array_ToRemove = new List<int>();
            foreach (KeyValuePair<int, ScriptArray> pair in arrayStorage)
            {
                if (pair.Value.mark != lastGCMark)
                {
                    array_ToRemove.Add(pair.Key);
                }
            }
            foreach (int key in array_ToRemove)
            {
                arrayStorage.Remove(key);
                //Console.WriteLine("removed array " + key);
            }

            List<int> table_ToRemove = new List<int>();
            foreach (KeyValuePair<int, Table> pair in tableStorage)
            {
                if (pair.Value.mark != lastGCMark)
                {
                    table_ToRemove.Add(pair.Key);
                    //Console.WriteLine("removed table " + pair.Key + " " + pair.Value.mark + " vs " + lastGCMark);
                }
            }
            foreach (int key in table_ToRemove)
            {
                tableStorage.Remove(key);
            }

            List<int> closure_ToRemove = new List<int>();
            foreach (KeyValuePair<int, Closure> pair in functionStorage)
            {
                if (pair.Value.mark != lastGCMark)
                {
                    closure_ToRemove.Add(pair.Key);
                    //Console.WriteLine("removed table " + pair.Key + " " + pair.Value.mark + " vs " + lastGCMark);
                }
            }
            foreach (int key in closure_ToRemove)
            {
                functionStorage.Remove(key);
            }

        }

        private void GC_FullCycle()
        {
            lastGCMark++;

            GC_MarkCollection(globalVars.Values);

            foreach (ClosureInstance instance in callStack)
            {
                GC_MarkCollection(instance.stackFrame);
            }

            GC_MarkValue(returnRegister);

            GC_MarkCollection(valueStack);

            //TODO add upvalues here

            GC_Sweep();
        }
    }
}
