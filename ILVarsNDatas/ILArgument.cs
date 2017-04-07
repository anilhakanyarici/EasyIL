using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILArgument : ILVariable
    {
        private int _argNo;
        private Type _argType;
        private bool _isByRef;
        private ILParameterInfo _parameterInfo;

        public ParameterAttributes Attributes { get { return this._parameterInfo.Attributes; } }
        public override Type ILType { get { if (this._argType.UnderlyingSystemType is GenericTypeParameterBuilder) { return this._argType.UnderlyingSystemType; } else return this._argType; } }
        public override bool IsBuilder { get { return this._argType is GenericTypeParameterBuilder; } }
        public bool IsRef { get { return this._isByRef && (this._parameterInfo.Attributes & ParameterAttributes.Out) != ParameterAttributes.Out; } }
        public bool IsOut { get { return this._isByRef && (this._parameterInfo.Attributes & ParameterAttributes.Out) == ParameterAttributes.Out; } }
        public override PinnedState PinnedState { get { return PinnedState.Argument; } }
        public string ParameterName { get { return this._parameterInfo.ParameterName; } }

        internal ILArgument(ILCoder coding, Type thisType) //ILThis or ILBase.
            : base(coding)
        {
            if (coding.CurrentMethod.IsStatic)
                throw new InvalidOperationException("This method is not instance.");

            this._argNo = 0;
            this._argType = thisType;
        }
        internal ILArgument(ILCoder coding, int argNo, Type argumentType)
            : base(coding)
        {
            this._parameterInfo = coding.CurrentMethod.GetParameterInfo(argNo);
            this._isByRef = argumentType.IsByRef;

            if (coding.CurrentMethod.IsStatic)
                this._argNo = argNo;
            else
                this._argNo = argNo + 1;

            if (this._isByRef)
                this._argType = argumentType.GetElementType();
            else
                this._argType = argumentType;
        }

        public override void AssignFrom(ILData ilValue)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (ilValue == null)
                throw new ArgumentNullException("ilValue");
            if (!this.ILType.IsAssignableFrom(ilValue.ILType))
                throw new TypeConvertException("Parametrenin türü, atanmak istenen objenin türünde değildi. Dönüştürme gerekli.");

            base.Coding.Generator.Emit(OpCodes.Ldarg, this._argNo);
            ((IILPusher)ilValue).Push();
            this.Coding.Generator.Emit(OpCodes.Stobj, ilValue.ILType);
        }

        protected override void Push()
        {
            base.Coding.Generator.Emit(OpCodes.Ldarg, this._argNo);
            if (this.IsRef)
                base.Generator.Emit(OpCodes.Ldobj, this.ILType);
        }
        protected override void PushAddress()
        {
            base.Coding.Generator.Emit(OpCodes.Ldarga, this._argNo);
        }
    }
}
