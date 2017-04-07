using NetworkIO.ILEmitter.Lazies;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public abstract class ILVariable : ILData
    {
        private static readonly MethodInfo _setArrayValue = typeof(Array).GetMethod("SetValue", new Type[] { typeof(object), typeof(int) });
        private static readonly MethodInfo _getArrayValue = typeof(Array).GetMethod("GetValue", new Type[] { typeof(int) });
        private static readonly MethodInfo _equals = typeof(object).GetMethod("Equals", new Type[] { typeof(object) });

        public abstract bool IsBuilder { get; }

        internal ILVariable(ILCoder coding)
            : base(coding)
        {

        }

        public abstract void AssignFrom(ILData ilValue);
        public virtual ILLazy Call(string methodName, Type[] genericArgs, ILData[] invokeParams)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (methodName == null)
                throw new ArgumentNullException("methodName");

            ILLazy push = null;
            Type[] parameterTypes = ILExtentionUtils.ParametersToTypeList(invokeParams);
            MethodInfo method = ILExtentionUtils.FindMethod(this.ILType, methodName, parameterTypes, false);
            if (method == null || method.IsStatic)
                throw new MethodNotFoundException("Hedef obje, çağrılan metodu içermiyor veya metot statik.");
            else
                push = new ILLazyInvoke(this.Coding, this, method, genericArgs, invokeParams);

            if (push.ILType == typeof(void))
            {
                ((IILPusher)push).Push();
                return null;
            }
            else
                return push;
        }
        public ILVariable Constrain(Type constrainedType)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.ILType.IsGenericParameter)
                return new ILConstrained(this.Coding, this, constrainedType);
            else
                throw new CodingException("Yalnızca generic parametre türüne sahip objeler sınırlandırılabilir.");
        }
        public ILLazy Equals(ILData ilValue)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (ilValue == null)
                throw new ArgumentNullException("ilValue");
            ILData box = this.Coding.Box(ilValue);
            return this.Coding.Invoke(ILVariable._equals, null, new ILData[] { this, box });
        }
        public virtual ILField GetField(string fieldName)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (fieldName == null)
                throw new ArgumentNullException("fieldName");

            FieldInfo field = this.ILType.GetField(fieldName);
            if (field == null || field.IsStatic)
                throw new FieldNotFoundException("Hedef obje, çağrılmak istenen alanı içermiyor veya alan statik.");
            else
                return new ILField(this.Coding, field, this);
        }
        public virtual ILProperty GetProperty(string propertyName)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");

            ILProperty ilProp = null;
            if (this.ILType.IsGenericParameter)
                throw new CodingException("Değişkenin türü bir generic parametre türündeydi. Generic parametreler üzerinden üye çağırmak için ILVariable.Constrain(Type) metodu kullanılmalıdır.");
            
            PropertyInfo property = this.ILType.GetProperty(propertyName);
            if (property == null || (ilProp = new ILProperty(this.Coding, this, property)).IsStatic)
                throw new MemberNotFoundException("Hedef obje, çağrılmak istenen özelliği içermiyor veya özellik statik.");
            else
                return ilProp;
        }
        public ILLazy Invoke(MethodInfo method, Type[] genericArgs, ILData[] invokeParams)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.PinnedState == ILEmitter.PinnedState.Base)
                throw new NotSupportedException("Bu metot base işaretçisi tarafından desteklenmiyor. This işaretçisi kullanılmalı.");
            if (method == null)
                throw new ArgumentNullException("method");
            if (method.GetType() != MethodBase.GetCurrentMethod().GetType())
                throw new ArgumentException("Çağrılan metot tutucusunun türü System.RuntimeMethod tipinde olmalı.");
            if (method.IsGenericMethod && (genericArgs == null || genericArgs.Length == 0))
                throw new ArgumentNullException("genericArgs", "Çağrılan yöntem bir generic tanımlama içeriyor.");

            ILLazy push = null;

            if (method.IsStatic)
                throw new CodingException("Çağrılan metot statik olmamalı.");
            else
                push = new ILLazyInvoke(this.Coding, this, method, genericArgs, invokeParams);

            if (push.ILType == typeof(void))
            {
                ((IILPusher)push).Push();
                return null;
            }
            else
                return push;
        }
        public ILLazy Invoke(ILMethodBuilder methodBuilder, Type[] genericArgs, ILData[] invokeParams)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.PinnedState == ILEmitter.PinnedState.Base)
                throw new NotSupportedException("Bu metot base işaretçisi tarafından desteklenmiyor. This işaretçisi kullanılmalı.");
            if (methodBuilder == null)
                throw new ArgumentNullException("methodBuilder");

            if (methodBuilder.MethodBuilder.IsGenericMethod && (genericArgs == null || genericArgs.Length == 0))
                throw new ArgumentNullException("genericArgs", "Çağrılan yöntem bir generic tanımlama içeriyor.");

            ILLazy push = null;

            if (methodBuilder.IsStatic)
                throw new CodingException("Çağrılan metot statik olmamalı.");
            else
                push = new ILLazyInvoke(this.Coding, this, methodBuilder, genericArgs, invokeParams);

            if (push.ILType == typeof(void))
            {
                ((IILPusher)push).Push();
                return null;
            }
            else
                return push;
        }
        public ILLazy IsNull()
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.ILType.IsClass)
                return this.Coding.Equals(this, this.Coding.Null);
            else
                throw new CodingException("Objenin türü referans değildi. Yalnızca referans türler null olabilir.");
        }
        public ILLazy LoadArrayLength()
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.ILType.IsArray)
                return new ILLazyLoadLength(this.Coding, this);
            else
                throw new CodingException("Obje bir dizi tipinde değildi.");
        }
        public ILLazy LoadElement(int index)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (index < 0)
                throw new IndexOutOfRangeException("İndisin değeri sıfırdan küçük olamaz.");

            if (this.ILType.IsArray)
                return new ILLazyLoadElement(this.Coding, this, index);
            else
                throw new CodingException("Obje bir dizi tipinde değildi.");
        }
        public ILLazy LoadElement(ILData index)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (index == null)
                throw new ArgumentNullException("index");

            if (this.ILType.IsArray)
            {
                if (index.ILType == typeof(int))
                    return new ILLazyLoadElement(this.Coding, this, index);
                else
                    throw new CodingException("İndis değerinin türü System.Int32 olmalı.");
            }
            else
                throw new CodingException("Obje bir dizi tipinde değildi.");
        }
        public ILLazy ToILString()
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            return this.Call("ToString", null, null);
        }
        public void StoreElement(int index, ILData ilObj)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (ilObj == null)
                throw new ArgumentNullException("ilObj");

            if (this.ILType.IsArray)
            {
                this.Push();
                ((IILPusher)ilObj).Push();
                this.Generator.Emit(OpCodes.Box, ilObj.ILType);
                this.Generator.Emit(OpCodes.Ldc_I4, index);
                this.Generator.Emit(OpCodes.Call, ILVariable._setArrayValue);
            }
            else
                throw new CodingException("Obje bir dizi tipinde değildi.");
        }
        public void StoreElement(ILData index, ILData ilObj)
        {
            if (((IBuilder)this.Coding).IsBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (ilObj == null)
                throw new ArgumentNullException("ilObj");
            if (index == null)
                throw new ArgumentNullException("index");

            if (this.ILType.IsArray)
            {
                if (index.ILType == typeof(int))
                {
                    this.Push();
                    ((IILPusher)ilObj).Push();
                    this.Generator.Emit(OpCodes.Box, ilObj.ILType);
                    ((IILPusher)index).Push();
                    this.Generator.Emit(OpCodes.Call, ILVariable._setArrayValue);
                }
                else
                    throw new CodingException("İndis değerinin türü System.Int32 olmalı.");
            }
            else
                throw new CodingException("Obje bir dizi tipinde değildi.");
        }

        private class ILConstrained : ILVariable
        {
            private Type _constainedType;
            private ILVariable _variable;

            public override Type ILType { get { return this._constainedType; } }
            public override bool IsBuilder { get { return this._variable.IsBuilder; } }
            public override PinnedState PinnedState { get { return this._variable.PinnedState; } }

            public ILConstrained(ILCoder coder, ILVariable variable, Type constrained)
                : base(coder)
            {
                this._constainedType = constrained;
                this._variable = variable;
            }
            public override void AssignFrom(ILData ilValue)
            {
                this._variable.AssignFrom(ilValue);
            }

            protected override void Push()
            {
                ((IILPusher)this._variable).Push();
            }
            protected override void PushAddress()
            {
                ((IILPusher)this._variable).PushAddress();
            }
        }
    }
}
