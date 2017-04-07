using NetworkIO.ILEmitter.Lazies;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    [Flags]
    public enum PinnedState { Null = 0, Local = 1, Field = 2, Constant = 4, Lazy = 8, Argument = 16, This = 48, Base = 80 }

    public abstract class ILData : IILPusher
    {
        public ILCoder Coding { get; private set; }
        public ILGenerator Generator { get; private set; }
        public abstract Type ILType { get; }
        public abstract PinnedState PinnedState { get; }

        internal ILData(ILCoder coding)
        {
            this.Coding = coding;
            this.Generator = coding.Generator;
        }

        public ILData Convert(Type to)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            else if (this.PinnedState == PinnedState.Null)
                throw new NotSupportedException("Yöntem, ILNull nesnesi için kullanılamaz.");
            else if (to == null)
                throw new ArgumentNullException("to");
            else if (to == this.ILType)
                return this;
            else if (to == typeof(object))
                return this.Coding.Box(this);
            else if (ILExtentionUtils.IsPrimitive(this.ILType) && ILExtentionUtils.IsPrimitive(to))
                return new ILLazyPrimitiveConverter(this.Coding, this, to);
            else if (this.ILType == typeof(object) && to != typeof(object))
                return this.Coding.Cast(CastOperations.UnboxAny, this, to);
            else if (this.ILType.IsGenericParameter && to.IsGenericParameter && to != this.ILType)
                return this.Coding.Cast(CastOperations.UnboxAny, this.Coding.Box(this), to);
            else if (this.ILType.IsClass && to.IsClass)
                return this.Coding.Cast(CastOperations.CastClass, this, to);
            else if (to.IsAssignableFrom(this.ILType))
            {
                if (this.ILType.IsValueType)
                {
                    ILLazy boxing = this.Coding.Box(this);
                    ILLazy unboxing = this.Coding.Cast(CastOperations.UnboxAny, boxing, to);
                    return unboxing;
                }
                else return this; //op_Explicit;
            }
            else
                throw new TypeConvertException("İki tip arasında dönüştürücü bulunamadı.");
        }
        protected abstract void Push();
        protected abstract void PushAddress();
        public ILLocal ToLocal()
        {
            if (this.PinnedState == PinnedState.Null)
                return this.ToLocal(typeof(object));
            return this.ToLocal(this.ILType);
        }
        public ILLocal ToLocal(Type variableType)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.PinnedState == PinnedState.Null)
            {
                if (variableType.IsClass)
                {
                    LocalBuilder local = this.Generator.DeclareLocal(variableType);
                    this.Push();
                    this.Generator.Emit(OpCodes.Stloc, local);
                    return new ILLocal(this.Coding, local);
                }
                else
                    throw new CodingException("Değer türündeki bir değişken Null olamaz.");
            }
            else
            {
                if (this.ILType == typeof(void))
                    throw new CodingException("Yerel değişkenin türü Void olamaz.");

                LocalBuilder local = this.Generator.DeclareLocal(variableType);
                this.Push();
                this.Generator.Emit(OpCodes.Stloc, local);
                return new ILLocal(this.Coding, local);
            }
        }

        void IILPusher.Push()
        {
            this.Push();
        }
        void IILPusher.PushAddress()
        {
            this.PushAddress();
        }
        void IILPusher.PushToString()
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.PinnedState == PinnedState.Base)
            {
                MethodInfo toString = this.ILType.BaseType.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public);
                ((IILPusher)this).Push();
                this.Generator.Emit(OpCodes.Call, toString);
            }
            else if (this.PinnedState == PinnedState.This)
            {
                MethodInfo toString = typeof(object).BaseType.GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public);
                if (this.ILType.IsClass)
                    ((IILPusher)this).Push();
                if (this.ILType.IsValueType)
                    ((IILPusher)this).PushAddress();
                this.Generator.Emit(OpCodes.Callvirt, toString);
            }
            else
            {
                MethodInfo toString = typeof(object).GetMethod("ToString", BindingFlags.Instance | BindingFlags.Public);
                if (this.ILType.IsGenericParameter)
                {
                    ((IILPusher)this).PushAddress();
                    this.Generator.Emit(OpCodes.Constrained, this.ILType);
                }
                else if (this.ILType.IsClass)
                    ((IILPusher)this).Push();
                else
                {
                    ((IILPusher)this.Coding.Box(this)).Push();
                }
                this.Generator.Emit(OpCodes.Callvirt, toString);
            }
        }
    }
}
