using System;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter.Lazies
{
    internal sealed class ILLazyPrimitiveOperator : ILLazy
    {
        private bool _isSingle;
        private ILData _left;
        private ILData _right;
        private DoubleOperators _dOperator;
        private SingleOperators _sOperator;
        private Type _dOpType;

        public override Type ILType
        {
            get
            {
                if (this._isSingle)
                    return this._right.ILType;
                else
                    return this._dOpType;
            }
        }

        public ILLazyPrimitiveOperator(ILCoder coding, ILData operand, SingleOperators singleOperator)
            : base(coding)
        {
            this._isSingle = true;
            this._right = operand;
            this._sOperator = singleOperator;
        }
        public ILLazyPrimitiveOperator(ILCoder coding, ILData left, DoubleOperators doubleOperator, ILData right, Type opResultType)
            : base(coding)
        {
            this._left = left;
            this._right = right;
            this._dOperator = doubleOperator;
            this._dOpType = opResultType;
        }

        protected override void Push()
        {
            if (this._isSingle)
            {
                ((IILPusher)this._right).Push();
                switch (this._sOperator)
                {
                    case SingleOperators.Plus:
                        break;
                    case SingleOperators.Not:
                        base.Generator.Emit(OpCodes.Not);
                        break;
                    case SingleOperators.Neg:
                        base.Generator.Emit(OpCodes.Neg);
                        break;
                    case SingleOperators.Increment:
                        if (this._right.ILType == typeof(double))
                            base.Generator.Emit(OpCodes.Ldc_R8, 1.0);
                        else if (this._right.ILType == typeof(float))
                            base.Generator.Emit(OpCodes.Ldc_R4, 1f);
                        else if (this._right.ILType == typeof(long) || this._right.ILType == typeof(ulong))
                            base.Generator.Emit(OpCodes.Ldc_I8, 1L);
                        else
                            base.Generator.Emit(OpCodes.Ldc_I4_1);
                        base.Generator.Emit(OpCodes.Add);
                        break;
                    case SingleOperators.Decrement:
                        if (this._right.ILType == typeof(double))
                            base.Generator.Emit(OpCodes.Ldc_R8, 1.0);
                        else if (this._right.ILType == typeof(float))
                            base.Generator.Emit(OpCodes.Ldc_R4, 1f);
                        else if (this._right.ILType == typeof(long) || this._right.ILType == typeof(ulong))
                            base.Generator.Emit(OpCodes.Ldc_I8, 1L);
                        else
                            base.Generator.Emit(OpCodes.Ldc_I4_1);
                        base.Generator.Emit(OpCodes.Sub);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                ILData left = this._left;
                ILData right = this._right;

                if (this._left.ILType != this.ILType)
                    left = left.Convert(this.ILType);
                if (this._right.ILType != this.ILType)
                    right = right.Convert(this.ILType);

                ((IILPusher)left).Push();
                ((IILPusher)right).Push();


                switch (this._dOperator)
                {
                    case DoubleOperators.Add:
                        base.Generator.Emit(OpCodes.Add);
                        break;
                    case DoubleOperators.Sub:
                        base.Generator.Emit(OpCodes.Sub);
                        break;
                    case DoubleOperators.Mul:
                        base.Generator.Emit(OpCodes.Mul);
                        break;
                    case DoubleOperators.Div:
                        if (this.ILType == typeof(int) || this.ILType == typeof(long))
                            base.Generator.Emit(OpCodes.Div);
                        else
                            base.Generator.Emit(OpCodes.Div_Un);
                        break;
                    case DoubleOperators.Rem:
                        if (this.ILType == typeof(int) || this.ILType == typeof(long))
                            base.Generator.Emit(OpCodes.Rem);
                        else
                            base.Generator.Emit(OpCodes.Rem_Un);
                        break;
                    case DoubleOperators.And:
                        base.Generator.Emit(OpCodes.And);
                        break;
                    case DoubleOperators.Or:
                        base.Generator.Emit(OpCodes.Or);
                        break;
                    case DoubleOperators.Xor:
                        base.Generator.Emit(OpCodes.Xor);
                        break;
                    case DoubleOperators.LShift:
                        base.Generator.Emit(OpCodes.Shl);
                        break;
                    case DoubleOperators.RShift:
                        if (this.ILType == typeof(int) || this.ILType == typeof(long))
                            base.Generator.Emit(OpCodes.Shr);
                        else
                            base.Generator.Emit(OpCodes.Shr_Un);
                        break;
                    default:
                        break;
                }
            }
        }
        protected override void PushAddress()
        {
            this.Push();
        }
    }
}
