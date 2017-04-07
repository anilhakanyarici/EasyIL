using System;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILLocal : ILVariable
    {
        public LocalBuilder EmitLocal { get; protected set; }
        public override Type ILType { get { return this.EmitLocal.LocalType; } }
        public override bool IsBuilder { get { return true; } }
        public override PinnedState PinnedState { get { return PinnedState.Local; } }

        internal ILLocal(ILCoder coding, LocalBuilder local)
            : base(coding)
        {
            if (local == null)
                throw new ArgumentNullException("local");

            this.EmitLocal = local;
        }

        public override void AssignFrom(ILData ilValue)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (ilValue == null)
                throw new ArgumentNullException("ilValue.");
            if (!this.ILType.IsAssignableFrom(ilValue.ILType))
                throw new TypeConvertException("Alanın türü, atanmak istenen objenin türünde değildi. Dönüştürme gerekli.");

            ((IILPusher)ilValue).Push();
            base.Generator.Emit(OpCodes.Stloc, this.EmitLocal);
        }

        protected override void Push()
        {
            base.Generator.Emit(OpCodes.Ldloc, this.EmitLocal);
        }
        protected override void PushAddress()
        {
            base.Generator.Emit(OpCodes.Ldloca, this.EmitLocal);
        }
    }
}
