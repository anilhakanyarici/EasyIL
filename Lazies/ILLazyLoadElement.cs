using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter.Lazies
{
    internal class ILLazyLoadElement : ILLazy
    {
        private static readonly MethodInfo getValue = typeof(Array).GetMethod("GetValue", new Type[] { typeof(int) });

        private ILVariable _array;
        private int _index;
        private ILData _ilIndex;

        public override Type ILType { get { return this._array.ILType.GetElementType(); } }

        public ILLazyLoadElement(ILCoder coding, ILVariable array, int index)
            : base(coding)
        {
            this._array = array;
            this._index = index;
        }
        public ILLazyLoadElement(ILCoder coding, ILVariable array, ILData index)
            : base(coding)
        {
            this._array = array;
            this._index = -1;
            this._ilIndex = index;
        }

        protected override void Push()
        {
            ((IILPusher)this._array).Push();
            if (this._index == -1)
                ((IILPusher)this._ilIndex).Push();
            else
                base.Generator.Emit(OpCodes.Ldc_I4, this._index);
            base.Generator.Emit(OpCodes.Call, ILLazyLoadElement.getValue);
            base.Generator.Emit(OpCodes.Unbox_Any, this.ILType);

        }
        protected override void PushAddress()
        {
            this.Push();
        }
    }
    internal class ILLazyLoadLength : ILLazy
    {
        private ILVariable _array;

        public override Type ILType { get { return typeof(uint); } }

        public ILLazyLoadLength(ILCoder coding, ILVariable array)
            : base(coding)
        {
            this._array = array;
        }

        protected override void Push()
        {
            ((IILPusher)this._array).Push();
            base.Generator.Emit(OpCodes.Ldlen);
        }
        protected override void PushAddress()
        {
            this.Push();
        }
    }
}
