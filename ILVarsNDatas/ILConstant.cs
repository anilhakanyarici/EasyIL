using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILConstant : ILData
    {
        private static MethodInfo typeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });

        private object _const;
        private Type _stackType;

        public override Type ILType { get { return this._stackType; } }
        public override PinnedState PinnedState { get { return PinnedState.Constant; } }

        public ILConstant(ILCoder coding, bool value) 
            : base(coding)
        {
            this._const = value;
            this._stackType = value.GetType();
        }
        internal ILConstant(ILCoder coding, int value)
            : base(coding)
        {
            this._const = value;
            this._stackType = value.GetType();
        }
        internal ILConstant(ILCoder coding, long value)
            : base(coding)
        {
            this._const = value;
            this._stackType = value.GetType();
        }
        internal ILConstant(ILCoder coding, float value)
            : base(coding)
        {
            this._const = value;
            this._stackType = value.GetType();
        }
        internal ILConstant(ILCoder coding, double value)
            : base(coding)
        {
            this._const = value;
            this._stackType = value.GetType();
        }
        internal ILConstant(ILCoder coding, string value)
            : base(coding)
        {
            this._const = value;
            this._stackType = value.GetType();
        }
        internal ILConstant(ILCoder coding, Type value)
            : base(coding)
        {
            this._const = value;
            this._stackType = value.GetType();
        }

        protected override void Push()
        {
            if (this._stackType == typeof(int))
                base.Generator.Emit(OpCodes.Ldc_I4, (int)this._const);
            else if (this._stackType == typeof(long))
                base.Generator.Emit(OpCodes.Ldc_I8, (long)this._const);
            else if (this._stackType == typeof(float))
                base.Generator.Emit(OpCodes.Ldc_R4, (float)this._const);
            else if (this._stackType == typeof(double))
                base.Generator.Emit(OpCodes.Ldc_R8, (double)this._const);
            else if (this._stackType == typeof(string))
                base.Generator.Emit(OpCodes.Ldstr, (string)this._const);
            else if (this._stackType == typeof(bool))
            {
                if ((bool)this._const)
                    base.Generator.Emit(OpCodes.Ldc_I4_1);
                else
                    base.Generator.Emit(OpCodes.Ldc_I4_0);
            }
            else
            {
                Type type = (Type)this._const;
                if (type.IsByRef)
                    type = type.GetElementType();
                this.Generator.Emit(OpCodes.Ldtoken, type);
                this.Generator.Emit(OpCodes.Call, ILConstant.typeFromHandle);
            }
        }
        protected override void PushAddress()
        {
            this.Push();
        }
    }
}
