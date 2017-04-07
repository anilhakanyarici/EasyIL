using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter.Lazies
{
    internal sealed class ILLazyPrimitiveConverter : ILLazy
    {
        private static readonly Type staticConvert = typeof(Convert);
        private static readonly MethodInfo changeType = ILLazyPrimitiveConverter.staticConvert.GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });

        private ILData _value;
        private Type _toType;

        public override Type ILType { get { return this._toType; } }

        public ILLazyPrimitiveConverter(ILCoder coding, ILData value, Type to)
            : base(coding)
        {
            this._toType = to;
            this._value = value;
        }

        protected override void Push()
        {
            ILConstant toType = base.Coding.TypeOf(this._toType);
            ((IILPusher)this._value).Push();
            base.Generator.Emit(OpCodes.Box, this._value.ILType);
            ((IILPusher)toType).Push();
            base.Generator.Emit(OpCodes.Call, ILLazyPrimitiveConverter.changeType);
            base.Generator.Emit(OpCodes.Unbox_Any, this.ILType);
        }
        protected override void PushAddress()
        {
            this.Push();
        }
    }
}
