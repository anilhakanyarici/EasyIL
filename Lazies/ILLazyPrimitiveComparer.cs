using System;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter.Lazies
{
    internal sealed class ILLazyPrimitiveComparer : ILLazy
    {
        private ILData _left;
        private ILData _right;
        private Comparisons _comparer;

        public Comparisons Comparison { get { return this._comparer; } }
        public override Type ILType { get { return typeof(bool); } }
        public ILData LeftOperand { get { return this._left; } }
        public ILData RightOperand { get { return this._right; } }

        internal ILLazyPrimitiveComparer(ILCoder coding, ILData left, Comparisons comparer, ILData right)
            : base(coding)
        {
            this._left = left;
            this._right = right;
            this._comparer = comparer;
        }

        protected override void Push()
        {
            ((IILPusher)this._left).Push();
            ((IILPusher)this._right).Push();
            if (this._comparer == Comparisons.Greater)
                base.Generator.Emit(OpCodes.Cgt);
            else if (this._comparer == Comparisons.Less)
                base.Generator.Emit(OpCodes.Clt);
            else if (this._comparer == Comparisons.Equal)
                base.Generator.Emit(OpCodes.Ceq);
            else if (this._comparer == Comparisons.GreatOrEqual)
            {
                base.Generator.Emit(OpCodes.Clt);
                base.Generator.Emit(OpCodes.Ldc_I4_0);
                base.Generator.Emit(OpCodes.Ceq);
            }
            else if (this._comparer == Comparisons.LessOrEqual)
            {
                base.Generator.Emit(OpCodes.Cgt);
                base.Generator.Emit(OpCodes.Ldc_I4_0);
                base.Generator.Emit(OpCodes.Ceq);
            }
            else //NotEqual
            {
                base.Generator.Emit(OpCodes.Ceq);
                base.Generator.Emit(OpCodes.Ldc_I4_0);
                base.Generator.Emit(OpCodes.Ceq);
            }
        }
        protected override void PushAddress()
        {
            this.Push();
        }
    }
}
