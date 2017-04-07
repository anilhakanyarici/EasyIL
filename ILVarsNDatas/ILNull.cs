using System;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILNull : ILData
    {
        private Type _type;

        public override Type ILType { get { return this._type; } }
        public override PinnedState PinnedState { get { return PinnedState.Null; } }

        internal ILNull(ILCoder coding, Type type)
            : base(coding)
        {
            this._type = type;
        }

        protected override void Push()
        {
            this.Coding.Generator.Emit(OpCodes.Ldnull);
        }
        protected override void PushAddress()
        {
            this.Push();
        }
    }
}
