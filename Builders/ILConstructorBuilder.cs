using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILConstructorBuilder : ILMethodBuilderBase, IBuilder
    {
        private ILCoder _coder;
        private List<ILParameterInfo> _parameters;
        private List<Type> _parameterTypes;
        private Dictionary<string, int> _nameToIndex;
        private MethodAttributes _methodAttributes;
        private CallingConventions _conventions;
        private bool _isBuild;

        public override MethodAttributes Attributes { get { return this._methodAttributes; } }
        public override int ArgumentsCount { get { return this._parameters.Count; } }
        public ConstructorBuilder ConstructorBuilder { get; private set; }
        public override ILMethodType MethodType { get { return ILMethodType.Constructor; } }
        public override bool IsStatic { get { return false; } }
        public override Type ReturnType { get { return typeof(void); } }

        internal ILConstructorBuilder(ILTypeBuilder typeBuilder, MethodAttributes attributes)
            : this(typeBuilder, attributes, CallingConventions.HasThis)
        {

        }
        internal ILConstructorBuilder(ILTypeBuilder typeBuilder, MethodAttributes attributes, CallingConventions conventions)
        {
            this._conventions = conventions;
            this._methodAttributes = attributes;
            base.ILTypeBuilder = typeBuilder;
            this._parameters = new List<ILParameterInfo>();
            this._parameterTypes = new List<Type>();
            this._nameToIndex = new Dictionary<string, int>();
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
        public override ILCoder GetCoder()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            if (this._coder == null)
            {
                this.ConstructorBuilder = base.ILTypeBuilder.TypeBuilder.DefineConstructor(this._methodAttributes, this._conventions, this.GetParameterTypes());
                for (int i = 0; i < this._parameters.Count; i++)
                {
                    ILParameterInfo parameter = this._parameters[i];
                    parameter.Builder = this.ConstructorBuilder.DefineParameter(i + 1, parameter.Attributes, parameter.ParameterName);

                    if (parameter.OptionalValue != null)
                        parameter.Builder.SetConstant(parameter.OptionalValue);
                }
                return this._coder = new ILCoder(this, this.ConstructorBuilder.GetILGenerator());
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

        void IBuilder.OnBuild()
        {
            if (this._coder == null)
                throw new BuildException("Soyut veya arabirim olmayan metotlar gövde içermelidir. Kurucu metodun gövdesi tanımlanmamış. Derleme yapılamaz.");

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

        
    }
}
