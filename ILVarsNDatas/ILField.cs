using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILField : ILVariable
    {
        private ILVariable _instance;

        public FieldInfo FieldInfo { get; private set; }
        public override bool IsBuilder { get { return this.FieldInfo is FieldBuilder; } }
        public bool IsStatic { get { return this._instance == null; } }
        public override Type ILType { get { return this.FieldInfo.FieldType; } }
        public override PinnedState PinnedState { get { return PinnedState.Field; } }

        internal ILField(ILCoder coding, FieldInfo info, ILVariable instance)
            : base(coding)
        {
            this.FieldInfo = info;
            this._instance = instance;
        }
       
        public override void AssignFrom(ILData ilValue)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (ilValue == null)
                throw new ArgumentNullException("ilValue");

            if (this.FieldInfo.FieldType.IsAssignableFrom(ilValue.ILType))
            {
                if (this.IsStatic)
                {
                    ((IILPusher)ilValue).Push();
                    this.Coding.Generator.Emit(OpCodes.Stsfld, this.FieldInfo);
                }
                else
                {
                    ((IILPusher)this._instance).Push();
                    ((IILPusher)ilValue).Push();
                    this.Coding.Generator.Emit(OpCodes.Stfld, this.FieldInfo);
                }
            }
            else
                throw new TypeConvertException("Alanın türü, atanmak istenen objenin türünde değildi. Dönüştürme gerekli.");
        }

        protected override void Push()
        {
            if (this.IsStatic)
                this.Generator.Emit(OpCodes.Ldsfld, this.FieldInfo);
            else
            {
                ((IILPusher)this._instance).Push();
                this.Generator.Emit(OpCodes.Ldfld, this.FieldInfo);
            }
                
        }
        protected override void PushAddress()
        {
            if (this.IsStatic)
                this.Generator.Emit(OpCodes.Ldsflda, this.FieldInfo);
            else
            {
                ((IILPusher)this._instance).Push();
                this.Generator.Emit(OpCodes.Ldflda, this.FieldInfo);
            }
        }
    }
}
