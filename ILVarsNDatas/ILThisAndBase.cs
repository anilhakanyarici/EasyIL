using NetworkIO.ILEmitter.Lazies;
using System;
using System.Reflection;


#pragma warning disable 0809
namespace NetworkIO.ILEmitter
{
    public class ILThis : ILArgument
    {
        public override PinnedState PinnedState { get { return PinnedState.This; } }

        internal ILThis(ILCoder coding)
            : base(coding, coding.CurrentMethod.ILTypeBuilder.TypeBuilder)
        {

        }

        [Obsolete("This işaretçisine atama yapılamaz.", true)]
        public override void AssignFrom(ILData ilValue)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            throw new CodingException("This işaretçisine atama yapılamaz.");
        }
        public override ILLazy Call(string methodName, Type[] genericArgs, ILData[] invokeParams)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (methodName == null)
                throw new ArgumentNullException("methodName");

            ILLazy push = null;

            Type[] parameterTypes = ILExtentionUtils.ParametersToTypeList(invokeParams);
            ILMethodBuilder builder = this.Coding.CurrentMethod.ILTypeBuilder.FindMethod(methodName, parameterTypes);
            if (builder == null)
                return this.Coding.Base.Call(methodName, genericArgs, invokeParams);
            if (builder.IsStatic)
                throw new MethodNotFoundException("Hedef obje, çağrılan metodu içermiyor veya metot statik.");
            else
                if (builder.IsGeneric && (genericArgs == null || genericArgs.Length == 0))
                    throw new ArgumentNullException("genericArgs", "Çağrılan yöntem bir generic tanımlama içeriyor.");
                else
                    push = new ILLazyInvoke(this.Coding, this, builder, genericArgs, invokeParams);

            if (push.ILType == typeof(void))
            {
                ((IILPusher)push).Push();
                return null;
            }
            else
                return push;
        }
        public override ILField GetField(string fieldName)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (fieldName == null)
                throw new ArgumentNullException("fieldName");

            FieldInfo field = this.Coding.CurrentMethod.ILTypeBuilder.FindFieldBuilder(fieldName);
            if (field == null)
                return this.Coding.Base.GetField(fieldName);
            if (field == null || field.IsStatic)
                throw new FieldNotFoundException("Hedef obje, çağrılmak istenen alanı içermiyor veya alan statik.");
            else
                return new ILField(this.Coding, field, this);
        }
        public override ILProperty GetProperty(string propertyName)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");

            ILPropertyBuilder builder = this.Coding.CurrentMethod.ILTypeBuilder.FindPropertyBuilder(propertyName);
            if (builder == null)
                return this.Coding.Base.GetProperty(propertyName);
            if (builder.IsStatic)
                throw new MemberNotFoundException("Hedef obje, çağrılmak istenen özelliği içermiyor veya özellik statik.");
            else
                return new ILProperty(this.Coding, this, builder);
        }
    }
    public class ILBase : ILArgument
    {
        public override PinnedState PinnedState { get { return PinnedState.Base; } }

        internal ILBase(ILCoder coding)
            : base(coding, coding.CurrentMethod.ILTypeBuilder.TypeBuilder)
        {

        }

        [Obsolete("Base işaretçisine atama yapılamaz.", true)]
        public override void AssignFrom(ILData ilValue)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            throw new CodingException("Base işaretçisine atama yapılamaz.");
        }
        public override ILLazy Call(string methodName, Type[] genericArgs, ILData[] invokeParams)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (methodName == null)
                throw new ArgumentNullException("methodName");

            ILLazy push = null;

            Type[] parameterTypes = ILExtentionUtils.ParametersToTypeList(invokeParams);
            Type baseType = this.Coding.CurrentMethod.ILTypeBuilder.BaseType;
            MethodInfo baseMethod = ILExtentionUtils.FindMethod(baseType, methodName, parameterTypes, false);
            if (baseMethod == null || baseMethod.IsStatic)
                throw new MethodNotFoundException("Hedef obje, çağrılan metodu içermiyor veya metot statik.");
            else
            {
                if ((baseMethod.Attributes & MethodAttributes.Private) == MethodAttributes.Private)
                    throw new MethodAccessException("Metot, private olarak tanımlanmış. Metoda erişilemiyor.");
                else
                    if (baseMethod.IsGenericMethod && (genericArgs == null || genericArgs.Length == 0))
                        throw new ArgumentNullException("genericArgs", "Çağrılan yöntem bir generic tanımlama içeriyor.");
                    else
                        push = new ILLazyInvoke(this.Coding, this, baseMethod, genericArgs, invokeParams);
            }
            if (push.ILType == typeof(void))
            {
                ((IILPusher)push).Push();
                return null;
            }
            else
                return push;
        }
        public override ILField GetField(string fieldName)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (fieldName == null)
                throw new ArgumentNullException("fieldName");

            Type baseType = this.Coding.CurrentMethod.ILTypeBuilder.BaseType;
            FieldInfo field = baseType.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null || field.IsStatic)
                throw new FieldNotFoundException("Hedef obje, çağrılmak istenen alanı içermiyor veya alan statik.");
            else
            {
                if ((field.Attributes & FieldAttributes.Private) == FieldAttributes.Private)
                    throw new FieldAccessException("Alan, private olarak tanımlanmış. Alana erişilemiyor.");
                else
                    return new ILField(this.Coding, field, this);
            }
        }
        public override ILProperty GetProperty(string propertyName)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");

            Type baseType = this.Coding.CurrentMethod.ILTypeBuilder.BaseType;
            ILProperty ilProp = null;
            PropertyInfo property = baseType.GetProperty(propertyName);
            if (property == null || (ilProp = new ILProperty(this.Coding, this, property)).IsStatic)
                throw new MemberNotFoundException("Hedef obje, çağrılmak istenen özelliği içermiyor veya özellik statik.");

            if (ilProp.GetModifier == AccessModifiers.Private)
                throw new MemberAccessException("Özellik, private olarak tanımlanmış. Özelliğe erişilemiyor.");
            else
                return ilProp;
        }
    }
}
