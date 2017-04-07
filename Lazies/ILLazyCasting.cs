using System;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter.Lazies
{
    internal class ILLazyCasting : ILLazy
    {
        private Type _castType;
        private CastOperations _op;
        private ILData _value;

        public override Type ILType { get { return this._castType; } }

        public ILLazyCasting(ILCoder coding, ILData value, CastOperations operations, Type related)
            : base(coding)
        {
            this._op = operations;
            this._value = value;

            if (operations == CastOperations.Box)
                this._castType = typeof(object);
            else
                this._castType = related;
        }
        protected override void Push()
        {
            ((IILPusher)this._value).Push();

            if (this._op == CastOperations.Box)
                base.Coding.Generator.Emit(OpCodes.Box, this._value.ILType);
            else if (this._op == CastOperations.Unbox)
                base.Coding.Generator.Emit(OpCodes.Unbox, this._castType);
            else if (this._op == CastOperations.UnboxAny)
                base.Coding.Generator.Emit(OpCodes.Unbox_Any, this._castType);
            else
                base.Coding.Generator.Emit(OpCodes.Castclass, this._castType);
        }

        protected override void PushAddress()
        {
            this.Push();
        }
    }
}
