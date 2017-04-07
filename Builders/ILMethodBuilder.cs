using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILMethodBuilder : ILMethodBuilderBase, IBuilder
    {
        private Dictionary<string, GenericTypeParameterBuilder> _genArgs;
        private List<Type> _parameterTypes;
        private List<ILParameterInfo> _parameters;
        private Dictionary<string, int> _nameToIndex;
        private ILCoder _coder;
        private MethodInfo _baseMethod;
        private bool _isBuild;
        private MethodAttributes _attributes;

        public override MethodAttributes Attributes { get { return this._attributes; } }
        public override int ArgumentsCount { get { return this._parameterTypes.Count; } }
        public MethodInfo BaseMethod { get { if (this.IsOverride) return this._baseMethod; else throw new MethodNotFoundException("Method was not override."); } }
        public override ILMethodType MethodType { get { return ILMethodType.Normal; } }
        public bool IsAbstract { get { return (this.Attributes & MethodAttributes.Abstract) == MethodAttributes.Abstract; } }
        public bool IsGeneric { get { return this._genArgs.Count > 0; } }
        public bool IsOverride { get; private set; }
        public override bool IsStatic { get { return (this.MethodBuilder.Attributes & MethodAttributes.Static) == MethodAttributes.Static; } }
        public MethodBuilder MethodBuilder { get; private set; }
        public AccessModifiers Modifier { get; private set; }
        public string Name { get; private set; }
        public override Type ReturnType { get { return this.MethodBuilder.ReturnType; } }

        internal ILMethodBuilder(ILTypeBuilder builder, MethodInfo baseMethod)
        {
            MethodAttributes attributes = baseMethod.Attributes;
            attributes &= ~MethodAttributes.Abstract;
            attributes &= ~MethodAttributes.VtableLayoutMask;
            attributes |= MethodAttributes.HideBySig;

            string methodName = baseMethod.Name;
            CallingConventions conventions = baseMethod.CallingConvention;
            this._attributes = attributes;

            this.MethodBuilder = builder.TypeBuilder.DefineMethod(methodName, attributes, conventions);
            this.ILTypeBuilder = builder;
            this.Name = baseMethod.Name;
            this._parameterTypes = new List<Type>();
            this._parameters = new List<ILParameterInfo>();
            this._nameToIndex = new Dictionary<string, int>();
            this._genArgs = new Dictionary<string, GenericTypeParameterBuilder>();

            if ((attributes & MethodAttributes.Public) == MethodAttributes.Public)
                this.Modifier = AccessModifiers.Public;
            else if (((attributes & MethodAttributes.FamANDAssem) == MethodAttributes.FamANDAssem))
                this.Modifier = AccessModifiers.Protected;
            else if (((attributes & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem))
                this.Modifier = AccessModifiers.ProtectedInternal;
            else if (((attributes & MethodAttributes.Assembly) == MethodAttributes.Assembly))
                this.Modifier = AccessModifiers.Internal;
            else if (((attributes & MethodAttributes.Family) == MethodAttributes.Family))
                this.Modifier = AccessModifiers.Protected;
            else
                this.Modifier = AccessModifiers.Private;

            this.CopyParametersFrom(baseMethod);
            this._baseMethod = baseMethod;
            this.IsOverride = true;
        }
        internal ILMethodBuilder(ILTypeBuilder builder, string methodName, MethodAttributes attributes)
        {
            this.MethodBuilder = builder.TypeBuilder.DefineMethod(methodName, attributes);
            this.ILTypeBuilder = builder;
            this._parameterTypes = new List<Type>();
            this._parameters = new List<ILParameterInfo>();
            this._nameToIndex = new Dictionary<string, int>();
            this._genArgs = new Dictionary<string, GenericTypeParameterBuilder>();
            this.Name = methodName;
            this._attributes = attributes;

            if ((attributes & MethodAttributes.Public) == MethodAttributes.Public)
                this.Modifier = AccessModifiers.Public;
            else if (((attributes & MethodAttributes.FamANDAssem) == MethodAttributes.FamANDAssem))
                this.Modifier = AccessModifiers.Protected;
            else if (((attributes & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem))
                this.Modifier = AccessModifiers.ProtectedInternal;
            else if (((attributes & MethodAttributes.Assembly) == MethodAttributes.Assembly))
                this.Modifier = AccessModifiers.Internal;
            else if (((attributes & MethodAttributes.Family) == MethodAttributes.Family))
                this.Modifier = AccessModifiers.Protected;
            else
                this.Modifier = AccessModifiers.Private;

            this.SetReturnType(typeof(void));
        }
        internal ILMethodBuilder(ILTypeBuilder builder, string methodName, MethodAttributes attributes, CallingConventions conventions)
        {
            this.MethodBuilder = builder.TypeBuilder.DefineMethod(methodName, attributes, conventions);
            this.ILTypeBuilder = builder;
            this._parameterTypes = new List<Type>();
            this._parameters = new List<ILParameterInfo>();
            this._nameToIndex = new Dictionary<string, int>();
            this._genArgs = new Dictionary<string, GenericTypeParameterBuilder>();
            this.Name = methodName;
            this._attributes = attributes;

            if ((attributes & MethodAttributes.Public) == MethodAttributes.Public)
                this.Modifier = AccessModifiers.Public;
            else if (((attributes & MethodAttributes.FamANDAssem) == MethodAttributes.FamANDAssem))
                this.Modifier = AccessModifiers.Protected;
            else if (((attributes & MethodAttributes.FamORAssem) == MethodAttributes.FamORAssem))
                this.Modifier = AccessModifiers.ProtectedInternal;
            else if (((attributes & MethodAttributes.Assembly) == MethodAttributes.Assembly))
                this.Modifier = AccessModifiers.Internal;
            else if (((attributes & MethodAttributes.Family) == MethodAttributes.Family))
                this.Modifier = AccessModifiers.Protected;
            else
                this.Modifier = AccessModifiers.Private;

            this.SetReturnType(typeof(void));
        }

        public void AddParameter(string genericTypeName, string name, ParameterAttributes attributes)
        {
            this.AddParameter(this.TypeOfGenericArgument(genericTypeName), name, attributes, null);
        }
        public void AddParameter(string genericTypeName, Type[] constraints, string name, ParameterAttributes attributes)
        {
            GenericTypeParameterBuilder parameterBuilder = this.TypeOfGenericArgument(genericTypeName) as GenericTypeParameterBuilder;
            this.AddParameter(parameterBuilder, name, attributes, null);

            if (constraints != null || constraints.Length > 0)
            {
                ILParameterInfo parameterInfo = this.GetParameterInfo(this._nameToIndex[name]);
                parameterInfo.Constraints = constraints.Clone() as Type[];
                if (constraints[0].IsClass)
                {
                    parameterBuilder.SetBaseTypeConstraint(constraints[0]);
                    Type[] interfaceConsts = new Type[constraints.Length - 1];
                    Array.Copy(constraints, 1, interfaceConsts, 0, interfaceConsts.Length);
                    if (interfaceConsts.Length > 0)
                        parameterBuilder.SetInterfaceConstraints(interfaceConsts);
                }
                else
                    parameterBuilder.SetInterfaceConstraints(constraints);
            }
        }
        public override void AddParameter(Type type, string name, ParameterAttributes attributes, object optionalValue)
        {
            if (this._coder != null)
                throw new DefinitionException("Metot gövdesi yazılmaya başlandıktan sonra parametre eklemesi yapılamaz.");
            if (this.IsOverride)
                throw new DefinitionException("Geçersiz kılınmış metotlar üzerine parametre eklemesi yapılamaz.");

            ILParameterInfo parameter = new ILParameterInfo() { Attributes = attributes, ParameterName = name, ParameterType = type, OptionalValue = optionalValue };
            this._nameToIndex.Add(name, this._parameters.Count);
            this._parameters.Add(parameter);
            this._parameterTypes.Add(type);
        }
        public bool ContainsGenericParameterName(string argName)
        {
            if (this._genArgs == null)
                return false;
            else
                return this._genArgs.ContainsKey(argName);
        }
        public void CopyParametersFrom(MethodInfo info)
        {
            if (this._coder != null)
                throw new DefinitionException("Metot gövdesi yazılmaya başlandıktan sonra parametre eklemesi yapılamaz.");

            if (this.IsOverride)
                throw new DefinitionException("Geçersiz kılınmış metotlar üzerine parametre eklemesi yapılamaz.");

            if (info.IsGenericMethod)
            {
                Type[] genericArgTypes = info.GetGenericArguments();
                string[] genericArgNames = new string[genericArgTypes.Length];
                for (int i = 0; i < genericArgTypes.Length; i++)
                    genericArgNames[i] = genericArgTypes[i].Name;
                this.DefineGenericParameters(genericArgNames);

                ParameterInfo[] methodParameters = info.GetParameters();
                for (int i = 0; i < methodParameters.Length; i++)
                {
                    ParameterInfo parameter = methodParameters[i];
                    Type parameterType = parameter.ParameterType;
                    Type[] constraints = null;
                    if (parameter.ParameterType.IsGenericParameter)
                        constraints = parameter.ParameterType.GetGenericParameterConstraints();
                    
                    string genericTypeName = null;

                    if (parameterType.IsArray)
                    {
                        Type elementType = parameterType.GetElementType();
                        int rank = parameterType.GetArrayRank();
                        if (this.ContainsGenericParameterName(elementType.Name))
                        {
                            Type copyParameterType = this.TypeOfGenericArgument(genericTypeName = elementType.Name).MakeArrayType(rank);
                            if (parameterType.IsByRef)
                                copyParameterType = copyParameterType.MakeByRefType();
                            this.AddParameter(copyParameterType, parameter.Name, parameter.Attributes);
                        }
                        else
                            this.AddParameter(parameterType, parameter.Name, parameter.Attributes);
                    }
                    else
                    {
                        if (this.ContainsGenericParameterName(parameterType.Name))
                        {
                            Type copyParameterType = this.TypeOfGenericArgument(genericTypeName = parameterType.Name);
                            if (parameterType.IsByRef)
                                copyParameterType = copyParameterType.MakeByRefType();
                            this.AddParameter(copyParameterType, parameter.Name, parameter.Attributes);
                        }

                        else
                        {
                            Type copyParameterType = parameterType;
                            this.AddParameter(copyParameterType, parameter.Name, parameter.Attributes);
                        }
                    }


                    if ((constraints != null && constraints.Length > 0) && genericTypeName != null)
                    {
                        ILParameterInfo parameterInfo = this.GetParameterInfo(this._nameToIndex[parameter.Name]);
                        parameterInfo.Constraints = constraints.Clone() as Type[];
                        GenericTypeParameterBuilder parameterBuilder = this.TypeOfGenericArgument(genericTypeName) as GenericTypeParameterBuilder;
                        if (constraints[0].IsClass)
                        {
                            parameterBuilder.SetBaseTypeConstraint(constraints[0]);
                            Type[] interfaceConsts = new Type[constraints.Length - 1];
                            Array.Copy(constraints, 1, interfaceConsts, 0, interfaceConsts.Length);
                            if (interfaceConsts.Length > 0)
                                parameterBuilder.SetInterfaceConstraints(interfaceConsts);
                        }
                        else
                            parameterBuilder.SetInterfaceConstraints(constraints);
                    }
                }

                Type returnType = info.ReturnType;
                Type[] returnConstraints = null;
                if (returnType.IsGenericParameter)
                    returnConstraints = returnType.GetGenericParameterConstraints();

                if (returnType.IsArray)
                {
                    Type elementType = returnType.GetElementType();
                    int rank = returnType.GetArrayRank();
                    if (this.ContainsGenericParameterName(elementType.Name))
                        this.SetReturnType(this.TypeOfGenericArgument(elementType.Name).MakeArrayType(rank));
                    else
                        this.SetReturnType(returnType);
                }
                else
                {
                    if (this.ContainsGenericParameterName(returnType.Name))
                        this.SetReturnType(this.TypeOfGenericArgument(returnType.Name));
                    else
                        this.SetReturnType(returnType);
                }

                if (returnConstraints != null && returnConstraints.Length > 0)
                {
                    GenericTypeParameterBuilder parameterBuilder = this.TypeOfGenericArgument(returnType.Name) as GenericTypeParameterBuilder;
                    if (returnConstraints[0].IsClass)
                    {
                        parameterBuilder.SetBaseTypeConstraint(returnConstraints[0]);
                        Type[] interfaceConsts = new Type[returnConstraints.Length - 1];
                        Array.Copy(returnConstraints, 1, interfaceConsts, 0, interfaceConsts.Length);
                        if (interfaceConsts.Length > 0)
                            parameterBuilder.SetInterfaceConstraints(interfaceConsts);
                    }
                    else
                        parameterBuilder.SetInterfaceConstraints(returnConstraints);
                }

            }
            else
            {
                ParameterInfo[] methodParameters = info.GetParameters();
                for (int i = 0; i < methodParameters.Length; i++)
                {
                    ParameterInfo parameter = methodParameters[i];
                    this.AddParameter(parameter.ParameterType, parameter.Name, parameter.Attributes);
                }
                Type returnType = info.ReturnType;
                this.SetReturnType(returnType);
            }
        }
        public void DefineGenericParameters(params string[] argNames)
        {
            if (this._coder != null)
                throw new DefinitionException("Metot gövdesi yazılmaya başlandıktan sonra parametre eklemesi yapılamaz.");
            if (this.IsOverride)
                throw new DefinitionException("Geçersiz kılınmış metotlar üzerine parametre eklemesi yapılamaz.");

            GenericTypeParameterBuilder[] genArgsBuilders = this.MethodBuilder.DefineGenericParameters(argNames);

            for (int i = 0; i < argNames.Length; i++)
                if (this._genArgs.ContainsKey(argNames[i]))
                    throw new BuildException("Bu parametre daha önce tanımlanmış. Aynı isimde yeni bir parametre tanımlaması yapılamaz.");
                else
                    this._genArgs.Add(argNames[i], genArgsBuilders[i]);
        }
        public override ILCoder GetCoder()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.IsAbstract)
                throw new CodingException("Soyut metotların gövdesi tanımlanamaz.");

            if (this._coder == null)
            {
                this.MethodBuilder.SetParameters(this._parameterTypes.ToArray());
                this.MethodBuilder.SetReturnType(this.ReturnType);
                for (int i = 0; i < this._parameterTypes.Count; i++)
                {
                    ILParameterInfo param = this._parameters[i];
                    if (this.MethodBuilder.IsStatic)
                        param.Builder = this.MethodBuilder.DefineParameter(i, param.Attributes, param.ParameterName);
                    else
                        param.Builder = this.MethodBuilder.DefineParameter(i + 1, param.Attributes, param.ParameterName);

                    if (param.OptionalValue != null)
                        param.Builder.SetConstant(param.OptionalValue);
                }
                this._coder = new ILCoder(this, this.MethodBuilder.GetILGenerator());
            }
            return this._coder;
        }
        public Type[] GetGenericArgumentTypes()
        {
            Type[] args = new Type[this._genArgs.Count];
            int index = 0;
            foreach (var item in this._genArgs.Values)
            {
                args[index] = item;
                index++;
            }
            return args;
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
                throw new IndexOutOfRangeException("Tanımlanan metot " + this.ArgumentsCount + " tane parametre içeriyor. Değer bundan büyük olamaz.");
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
        public void SetCustomAttributes(CustomAttributeBuilder[] attributes)
        {
            if (this._coder != null)
                throw new BuildException("Metodun tanımlandığı tip derlenmiş. Derlemesi yapılan tiplerde değişiklik yapılamaz.");

            for (int i = 0; i < attributes.Length; i++)
                this.MethodBuilder.SetCustomAttribute(attributes[i]);
        }
        public void SetReturnType(Type type)
        {
            if (this._coder != null)
                throw new BuildException("Metodun tanımlandığı tip derlenmiş. Derlemesi yapılan tiplerde değişiklik yapılamaz.");
            if (this.IsOverride)
                throw new BuildException("Geçersiz kılınmış metotlarda geri dönüş türü ayarlanamaz.");

            this.MethodBuilder.SetReturnType(type);
        }
        public Type TypeOfGenericArgument(string argName)
        {
            try { return this._genArgs[argName]; }
            catch (ArgumentException) { throw new MemberNotFoundException(argName + " isimli generic argüman bulunamadı."); }
        }

        void IBuilder.OnBuild()
        {
            if (this._coder == null && !this.IsAbstract)
                throw new BuildException("Soyut veya arabirim olmayan metotlar gövde içermelidir." + this.Name + " isimli metodun gövdesi tanımlanmamış. Derleme yapılamaz.");

            ((IBuilder)this._coder).OnBuild();
            this._isBuild = true;
            this._coder = null;
            this._genArgs.Clear();
            this._genArgs = null;
            this._parameterTypes.Clear();
            this._parameterTypes = null;
            this._parameters.Clear();
            this._parameters = null;
            this._nameToIndex.Clear();
            this._nameToIndex = null;
        }
        bool IBuilder.IsBuild { get { return this._isBuild; } }
    }
}
