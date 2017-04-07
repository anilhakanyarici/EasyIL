using NetworkIO.ILEmitter.Lazies;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILProperty
    {
        private ILVariable _instance;
        private MethodInfo _getMethod;
        private MethodInfo _setMethod;
        private ILMethodBuilder _getMethodBuilder;
        private ILMethodBuilder _setMethodBuilder;

        public ILCoder Coding { get; private set; }
        public ILGenerator Generator { get; private set; }
        public AccessModifiers GetModifier { get; private set; }
        public bool IsStatic { get { return this._instance == null; } }
        public AccessModifiers PropertyModifier { get { if (this.GetModifier > this.SetModifier) return GetModifier; else return this.SetModifier; } }
        public Type PropertyType { get; private set; }
        public AccessModifiers SetModifier { get; private set; }

        internal ILProperty(ILCoder coding, ILVariable instance, PropertyInfo propertyInfo)
        {
            this.Coding = coding;
            this._instance = instance;
            this._getMethod = propertyInfo.GetGetMethod();
            this._setMethod = propertyInfo.GetSetMethod();
            this.PropertyType = propertyInfo.PropertyType;

            if (this._getMethod != null)
            {
                if ((this._getMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                    this.GetModifier = AccessModifiers.Public;
                else if (((this._getMethod.Attributes & MethodAttributes.FamANDAssem) == MethodAttributes.FamANDAssem))
                    this.GetModifier = AccessModifiers.Protected;
                else if (((this._getMethod.Attributes & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem))
                    this.GetModifier = AccessModifiers.ProtectedInternal;
                else if (((this._getMethod.Attributes & MethodAttributes.Assembly) == MethodAttributes.Assembly))
                    this.GetModifier = AccessModifiers.Internal;
                else if (((this._getMethod.Attributes & MethodAttributes.Family) == MethodAttributes.Family))
                    this.GetModifier = AccessModifiers.Protected;
                else
                    this.GetModifier = AccessModifiers.Private;
            }

            if (this._setMethod != null)
            {
                if ((this._setMethod.Attributes & MethodAttributes.Public) == MethodAttributes.Public)
                    this.SetModifier = AccessModifiers.Public;
                else if (((this._setMethod.Attributes & MethodAttributes.FamANDAssem) == MethodAttributes.FamANDAssem))
                    this.SetModifier = AccessModifiers.Protected;
                else if (((this._setMethod.Attributes & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem))
                    this.SetModifier = AccessModifiers.ProtectedInternal;
                else if (((this._setMethod.Attributes & MethodAttributes.Assembly) == MethodAttributes.Assembly))
                    this.SetModifier = AccessModifiers.Internal;
                else if (((this._setMethod.Attributes & MethodAttributes.Family) == MethodAttributes.Family))
                    this.SetModifier = AccessModifiers.Protected;
                else
                    this.SetModifier = AccessModifiers.Private;
            }

        }
        internal ILProperty(ILCoder coding, ILVariable instance, ILPropertyBuilder propertyBuilder)
        {
            this.Coding = coding;
            this._instance = instance;
            this._getMethodBuilder = propertyBuilder.GetMethod;
            this._setMethodBuilder = propertyBuilder.SetMethod;
            this.PropertyType = propertyBuilder.PropertyType;

            this.GetModifier = propertyBuilder.GetMethod == null ? AccessModifiers.Private : propertyBuilder.GetModifier;
            this.SetModifier = propertyBuilder.SetMethod == null ? AccessModifiers.Private : propertyBuilder.SetModifier;
        }

        public ILLazy Get()
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this._getMethod == null && this._getMethodBuilder == null)
                throw new MethodNotFoundException("Özellik, get metodunu içermiyor.");
            else
            {
                if (this._getMethod == null)
                    return new ILLazyInvoke(this.Coding, this._instance, this._getMethodBuilder, null, null);
                else
                    return new ILLazyInvoke(this.Coding, this._instance, this._getMethod, null, null);
            }
        }
        public void Set(ILData ilValue)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (ilValue == null)
                throw new ArgumentNullException("ilValue");

            if (this._setMethod == null && this._setMethodBuilder == null)
                throw new MethodNotFoundException("Özellik, set metodunu içermiyor.");
            else if (ilValue.ILType != this.PropertyType)
                throw new TypeConvertException("Atanan tür ile özelliğin türü uyumsuz.");
            else
            {
                if (this._setMethod == null)
                    ((IILPusher)new ILLazyInvoke(this.Coding, this._instance, this._setMethodBuilder, null, new ILData[] { ilValue })).Push();
                else
                    ((IILPusher)new ILLazyInvoke(this.Coding, this._instance, this._setMethod, null, new ILData[] { ilValue })).Push();
            }
        }
    }
}
