using System.Collections.Generic;

namespace CoolLanguage.VM
{
    enum dataType
    {
        Null,
        Boolean,
        Number,
        String,
        Table,
        Array,
        Function,
        CFunction
    }

    struct ScriptValue
    {
        static string[] typeNames =
        {
            "null",
            "boolean",
            "number",
            "string",
            "table",
            "array",
            "function",
            "cfunction"
        };

        public dataType type;

        public object value;

        public ScriptValue(dataType t, object v = null)
        {
            type = t;
            value = v;
        }

        public override string ToString()
        {
            if (type == dataType.Table)
            {
                return "table: " + value;
            }
            else if (type == dataType.Array)
            {
                return "array: " + value;
            }
            else if (type == dataType.Function)
            {
                return "function: " + value;
            }
            else if (type == dataType.CFunction)
            {
                return "cfunction: " + value;
            }
            else if (type == dataType.Boolean)
            {
                return (bool)value ? "true" : "false";
            }
            else if (type == dataType.Null)
            {
                return "null";
            }

            return value.ToString();
        }

        public string TypeName => typeNames[(int)type];

        public static ScriptValue Null = new ScriptValue(dataType.Null);
    }

    class Table
    {
        public byte mark = 0;
        public Dictionary<string, ScriptValue> dictionary = new Dictionary<string, ScriptValue>();
    }

    class ScriptArray
    {
        public byte mark = 0;
        public List<ScriptValue> list = new List<ScriptValue>();
    }

    class FunctionPrototype
    {
        public int stackSize;

        public int argCount;

        public VMInstruction[] instructions;

        public FunctionPrototype(VMInstruction[] insts, int _stackSize, int argCount = 0)
        {
            instructions = insts;

            stackSize = _stackSize;

            this.argCount = argCount;
        }
    }

    class Closure
    {
        public FunctionPrototype prototype;

        public byte mark = 0;

        public Closure(FunctionPrototype prototype)
        {
            this.prototype = prototype;
        }
    }
}