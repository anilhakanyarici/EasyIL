using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILDynamicMethod : ILMethodBuilderBase, IBuilder
    {
        private MethodAttributes _attributes;
        private List<ILParameterInfo> _parameters;
        private List<Type> _parameterTypes;
        private Dictionary<string, int> _nameToIndex;
        private Type _returnType;
        private CallingConventions _conventions;
        private bool _isBuild;
        private bool _isImplemented;
        private ILCoder _coder;
        private DynamicMethod _dynMethod;

        public override int ArgumentsCount { get { return this._parameters.Count; } }
        public override MethodAttributes Attributes { get { return this._attributes; } }
        public Type DeclaringType { get; private set; }
        public DynamicMethod DynamicMethod { get { if (!this._isImplemented) throw new NotImplementedException("Dynamic method was not implemented. Use GetCoding() once."); else return this._dynMethod; } }
        public override ILTypeBuilder ILTypeBuilder { get { throw new NotSupportedException("ILTypeBuilder property was not supported for the Dynamic methods."); } protected set { throw new NotSupportedException("ILTypeBuilder property was not supported for the Dynamic methods."); } }
        public override bool IsStatic { get { return true; } }
        public override ILMethodType MethodType { get { return ILMethodType.Dynamic; } }
        public string Name { get; private set; }
        public override Type ReturnType { get { return this._returnType; } }

        public ILDynamicMethod(string name)
            : this(name, typeof(DynamicMethod))
        {

        }
        public ILDynamicMethod(string name, Type ownerType)
        {
            this.Name = name;
            this._attributes = MethodAttributes.Static | MethodAttributes.Public;
            this._parameters = new List<ILParameterInfo>();
            this._parameterTypes = new List<Type>();
            this._nameToIndex = new Dictionary<string, int>();
            this._returnType = typeof(void);
            this._conventions = CallingConventions.Standard;
            this.DeclaringType = ownerType;
        }

        public override void AddParameter(Type type, string name, ParameterAttributes attributes, object optionalValue)
        {
            if (this._coder != null)
                throw new DefinitionException("Metot gövdesi yazılmaya başlandıktan sonra parametre eklemesi yapılamaz.");

            ILParameterInfo parameter = new ILParameterInfo { Attributes = attributes, ParameterName = name, ParameterType = type, OptionalValue = optionalValue };
            this._nameToIndex.Add(name, this._parameterTypes.Count);
            this._parameterTypes.Add(type);
            this._parameters.Add(parameter);
        }
        public Delegate CreateDelegate(Type delegateType)
        {
            if (!this._isBuild)
                ((IBuilder)this).OnBuild();

            return this.DynamicMethod.CreateDelegate(delegateType);
        }
        public override ILCoder GetCoder()
        {
            if (this._isBuild)
                throw new CodingException("Metot daha önce derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            if (this._coder == null)
            {
                this._isImplemented = true;
                this._dynMethod = new DynamicMethod(this.Name, this._attributes, this._conventions, this.ReturnType, this.GetParameterTypes(), this.DeclaringType, false);
                for (int i = 0; i < this._parameters.Count; i++)
                {
                    ILParameterInfo parameter = this._parameters[i];
                    parameter.Builder = this.DynamicMethod.DefineParameter(i + 1, parameter.Attributes, parameter.ParameterName);

                    if (parameter.OptionalValue != null)
                        parameter.Builder.SetConstant(parameter.OptionalValue);
                }
                return this._coder = new ILCoder(this, this.DynamicMethod.GetILGenerator());
            }
            else
                return this._coder;
        }
        public override ILParameterInfo GetParameterInfo(int parameterIndex)
        {
            if (parameterIndex >= this.ArgumentsCount)
                throw new IndexOutOfRangeException("Tanımanan metot " + this.ArgumentsCount + " tane parametre içeriyor. Değer bundan büyük olamaz.");
            else if (parameterIndex < 0)
                throw new IndexOutOfRangeException("Parametre indisi sıfırdan küçük olamaz.");
            else
                return this._parameters[parameterIndex];
        }
        public override Type GetParameterType(int parameterIndex)
        {
            if (parameterIndex >= this.ArgumentsCount)
                throw new IndexOutOfRangeException("Tanımanan metot " + this.ArgumentsCount + " tane parametre içeriyor. Değer bundan büyük olamaz.");
            else if (parameterIndex < 0)
                throw new IndexOutOfRangeException("Parametre indisi sıfırdan küçük olamaz.");
            else
                return this._parameterTypes[parameterIndex];
        }
        public override Type[] GetParameterTypes()
        {
            return this._parameterTypes.ToArray();
        }
        public override int ParameterNameToIndex(string parameterName)
        {
            if (parameterName == null)
                throw new ArgumentNullException("Parametre ismi boş olamaz.");

            if (this._nameToIndex.ContainsKey(parameterName))
                return this._nameToIndex[parameterName];
            else
                throw new MemberNotFoundException(parameterName + " isimli parametre bulunamadı.");
        }
        public void SetReturnType(Type returnType)
        {
            if (returnType.IsByRef)
                throw new NotSupportedException("Geri dönüş tipi dinamik metotlarda referans olamaz.");
            else
                this._returnType = returnType;
        }

        void IBuilder.OnBuild()
        {
            if (this._coder == null)
                throw new BuildException("Soyut veya arabirim olmayan metotlar gövde içermelidir." + this.Name + " isimli metodun gövdesi tanımlanmamış. Derleme yapılamaz.");

            ((IBuilder)this._coder).OnBuild();
            this._isBuild = true;
            this._coder = null;
            this._parameters.Clear();
            this._parameters = null;
            this._nameToIndex.Clear();
            this._nameToIndex = null;
            this._parameterTypes.Clear();
            this._parameterTypes = null;
        }
        bool IBuilder.IsBuild { get { return this._isBuild; } }

        private class DynamicAttributeProvider : ICustomAttributeProvider
        {
            private List<object> _attributes;
            private object[] _inherited;

            public DynamicAttributeProvider(MethodInfo baseMethod)
            {
                this._attributes = new List<object>();
                if (baseMethod == null)
                    this._inherited = new object[0];
                else
                    this._inherited = baseMethod.GetCustomAttributes(true);
            }

            public object[] GetCustomAttributes(bool inherit)
            {
                if (inherit)
                {
                    object[] attribs = new object[this._inherited.Length + this._attributes.Count];
                    Array.Copy(this._inherited, 0, attribs, 0, this._inherited.Length);
                    object[] thisAttribs = this._attributes.ToArray();
                    Array.Copy(thisAttribs, 0, attribs, this._inherited.Length, thisAttribs.Length);
                    return attribs;
                }
                else
                    return this._attributes.ToArray();
            }
            public object[] GetCustomAttributes(Type attributeType, bool inherit)
            {
                List<object> demand = new List<object>();

                for (int i = 0; i < this._attributes.Count; i++)
                {
                    object attr = this._attributes[i];
                    if (attr.GetType() == attributeType)
                        demand.Add(attr);
                }

                if (inherit)
                {
                    for (int i = 0; i < this._inherited.Length; i++)
                    {
                        object attr = this._inherited[i];
                        if (attr.GetType() == attributeType)
                            demand.Add(attr);
                    }
                }
                return demand.ToArray();
            }
            public bool IsDefined(Type attributeType, bool inherit)
            {
                for (int i = 0; i < this._attributes.Count; i++)
                {
                    object attr = this._attributes[i];
                    if (attr.GetType() == attributeType)
                        return true;
                }

                if (inherit)
                {
                    for (int i = 0; i < this._inherited.Length; i++)
                    {
                        object attr = this._inherited[i];
                        if (attr.GetType() == attributeType)
                            return true;
                    }
                }
                return false;
            }
            public void SetAttribute(Attribute attribute)
            {
                AttributeUsageAttribute usage = attribute.GetType().GetCustomAttributes(typeof(AttributeUsageAttribute), false)[0] as AttributeUsageAttribute;
                if (!usage.AllowMultiple && this.IsDefined(attribute.GetType(), usage.Inherited))
                    throw new InvalidOperationException("Attribute not allow multiple.");
                else
                    this._attributes.Add(attribute);
            }
        }

        
    }
}
