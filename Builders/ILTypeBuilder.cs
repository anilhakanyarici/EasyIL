using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public enum AccessModifiers { Public = 4, Protected = 1, ProtectedInternal = 3, Internal = 2, Private = 0 }
    public class ILTypeBuilder : IGenericBuilder, IBuilder
    {
        private List<ILConstructorBuilder> _ctors;
        private Dictionary<string, FieldBuilder> _fields;
        private Dictionary<string, ILPropertyBuilder> _properties;
        private Type _compiledType;
        private Type[] _interfaces;
        private Dictionary<string, GenericTypeParameterBuilder> _genArgs;
        private Dictionary<string, List<ILMethodBuilder>> _definedMethods;
        private ILTypeBuilder _owner;
        private bool _isBuild;
        private bool _setBaseType;

        public Type BaseType { get { return this.TypeBuilder.BaseType; } }
        public string FullName { get { if (this.IsNestedType) return this._owner.FullName + "." + this.Name; else return this.Name; } }
        public ILAssemblyBuilder ILAssemblyBuilder { get; private set; }
        public bool IsNestedType { get; private set; }
        public string Name { get; private set; }
        public ILTypeBuilder OwnerTypeBuilder { get { return this._owner; } }
        public TypeBuilder TypeBuilder { get; private set; }

        internal ILTypeBuilder(ILAssemblyBuilder builder, string typeName, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            Type runtime = typeof(Type).GetType();

            if (baseType.GetType() != runtime)
                throw new ArgumentException("Temel sınıf tür tanımlayıcısı, System.RuntimeType tipinde olmalı.");
            if (baseType.IsInterface)
                throw new BuildException("Arabirimler temel sınıf olarak kullanılamaz.");
            if (baseType.IsAbstract && baseType.IsSealed)
                throw new BuildException("Static sınıflar temel sınıf olarak kullanılamaz.");
            if (baseType.IsSealed)
                throw new BuildException("Mühürlenmiş sınıflar temel sınıf olarak kullanılamaz.");

            this.ILAssemblyBuilder = builder;
            if (baseType == null)
                this.TypeBuilder = builder.ModuleBuilder.DefineType(typeName, attributes);
            else
                this.TypeBuilder = builder.ModuleBuilder.DefineType(typeName, attributes, baseType);

            if (interfaces != null && interfaces.Length > 0)
            {
                for (int i = 0; i < interfaces.Length; i++)
                {
                    Type interfaceType = interfaces[i];
                    if (interfaceType.IsInterface)
                        if (interfaceType.GetType() == runtime)
                            this.TypeBuilder.AddInterfaceImplementation(interfaceType);
                        else
                            throw new BuildException("Yerleştirilen arabirim tanımlayıcılarının hepsi System.RuntimeType türünden olmalıdır.");
                    else
                        throw new BuildException("Yerleştirilen arabirimlerden birisi bir arabirim değildi. Geçersiz arabirim ismi " + interfaceType.Name);
                }
            }
            this._fields = new Dictionary<string, FieldBuilder>();
            if (interfaces == null)
                this._interfaces = new Type[0];
            else
                this._interfaces = interfaces;
            this._genArgs = new Dictionary<string, GenericTypeParameterBuilder>();
            this._properties = new Dictionary<string, ILPropertyBuilder>();
            this._definedMethods = new Dictionary<string, List<ILMethodBuilder>>();
            this._ctors = new List<ILConstructorBuilder>();
            this.Name = typeName;
            this._setBaseType = !(baseType == null || baseType == typeof(object));
        }
        /// <summary>
        /// Defining NestedType.
        /// </summary>
        internal ILTypeBuilder(ILTypeBuilder builder, string typeName, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            this._owner = builder;
            Type runtime = typeof(Type).GetType();

            if (baseType.GetType() != runtime)
                throw new ArgumentException("Temel sınıf tür tanımlayıcısı, System.RuntimeType tipinde olmalı.");
            if (baseType.IsInterface)
                throw new BuildException("Arabirimler temel sınıf olarak kullanılamaz.");
            if (baseType.IsAbstract && baseType.IsSealed)
                throw new BuildException("Static sınıflar temel sınıf olarak kullanılamaz.");
            if (baseType.IsSealed)
                throw new BuildException("Mühürlenmiş sınıflar temel sınıf olarak kullanılamaz.");

            this.ILAssemblyBuilder = builder.ILAssemblyBuilder;
            if (baseType == null)
                this.TypeBuilder = builder.TypeBuilder.DefineNestedType(typeName, attributes);
            else
                this.TypeBuilder = builder.TypeBuilder.DefineNestedType(typeName, attributes, baseType);

            if (interfaces != null && interfaces.Length > 0)
            {
                for (int i = 0; i < interfaces.Length; i++)
                {
                    Type interfaceType = interfaces[i];
                    if (interfaceType.IsInterface)
                        if (interfaceType.GetType() == runtime)
                            this.TypeBuilder.AddInterfaceImplementation(interfaceType);
                        else
                            throw new BuildException("Yerleştirilen arabirim tanımlayıcılarının hepsi System.RuntimeType türünden olmalıdır.");
                    else
                        throw new BuildException("Yerleştirilen arabirimlerden birisi bir arabirim değildi. Geçersiz arabirim ismi " + interfaceType.Name);
                }
            }
            this._fields = new Dictionary<string, FieldBuilder>();
            if (interfaces == null)
                this._interfaces = new Type[0];
            else
                this._interfaces = interfaces;
            this.IsNestedType = true;
            this._genArgs = new Dictionary<string, GenericTypeParameterBuilder>();
            this._properties = new Dictionary<string, ILPropertyBuilder>();
            this._definedMethods = new Dictionary<string, List<ILMethodBuilder>>();
            this._ctors = new List<ILConstructorBuilder>();
            this.Name = typeName;
            this._setBaseType = !(baseType == null || baseType == typeof(object));
        }

        public Type CompileType()
        {
            if (this._compiledType == null)
            {
                if (this._isBuild)
                    return this.ILAssemblyBuilder.GetType(this.FullName);
                this._compiledType = this.ILAssemblyBuilder.CompileType(this);
                return this._compiledType;
            }
            else
                return this._compiledType;
        }
        public bool ContainsGenericParameterName(string argName)
        {
            if (this._genArgs == null)
                return false;
            else
                return this._genArgs.ContainsKey(argName);
        }
        public object CreateInstance(params object[] args)
        {
            Type compiledType = this.CompileType();
            return Activator.CreateInstance(compiledType, args);
        }
        public TBase CreateInstance<TBase>(params object[] args)
        {
            return (TBase)Activator.CreateInstance(this.CompileType(), args);
        }
        public ILConstructorBuilder DefineConstructor(AccessModifiers modifier)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            ILConstructorBuilder builder;
            switch (modifier)
            {
                case AccessModifiers.Public:
                    builder = new ILConstructorBuilder(this, MethodAttributes.Public);
                    break;
                case AccessModifiers.Protected:
                    builder = new ILConstructorBuilder(this, MethodAttributes.Family);
                    break;
                case AccessModifiers.ProtectedInternal:
                    builder = new ILConstructorBuilder(this, MethodAttributes.FamORAssem);
                    break;
                case AccessModifiers.Internal:
                    builder = new ILConstructorBuilder(this, MethodAttributes.Assembly);
                    break;
                default: //Private
                    builder = new ILConstructorBuilder(this, MethodAttributes.Private);
                    break;
            }
            this._ctors.Add(builder);
            return builder;
        }
        public ILConstructorBuilder DefineConstructor(MethodAttributes attributes)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            ILConstructorBuilder builder = new ILConstructorBuilder(this, attributes);
            this._ctors.Add(builder);
            return builder;
        }
        public ILConstructorBuilder DefineConstructor(MethodAttributes attributes, CallingConventions conventions)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            ILConstructorBuilder builder = new ILConstructorBuilder(this, attributes, conventions);
            this._ctors.Add(builder);
            return builder;
        }
        public FieldBuilder DefineField(Type fieldType, string fieldName, FieldAttributes attributes)
        {
            return this.DefineField(fieldType, fieldName, attributes, null);
        }
        public FieldBuilder DefineField(Type fieldType, string fieldName, FieldAttributes attributes, object defaultValue)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            if (this._fields.ContainsKey(fieldName))
                throw new DefinitionException(fieldName + " isimli alan daha önce tanımlanmış.");

            FieldBuilder builder = this.TypeBuilder.DefineField(fieldName, fieldType, attributes);
            this._fields.Add(fieldName, builder);
            if (defaultValue != null)
                builder.SetConstant(defaultValue);
            return builder;
        }
        public void DefineGenericParameters(params string[] argNames)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            GenericTypeParameterBuilder[] genArgsBuilders = this.TypeBuilder.DefineGenericParameters(argNames);

            if (this._genArgs == null)
                this._genArgs = new Dictionary<string, GenericTypeParameterBuilder>();

            for (int i = 0; i < argNames.Length; i++)
                if (this._genArgs.ContainsKey(argNames[i]))
                    throw new DefinitionException("Bu parametre daha önce tanımlanmış. Aynı isimde yeni bir parametre tanımlaması yapılamaz.");
                else
                    this._genArgs.Add(argNames[i], genArgsBuilders[i]);
        }
        public ILMethodBuilder DefineMethod(string methodName, AccessModifiers modifier)
        {
            return this.DefineMethod(methodName, modifier, false);
        }
        public ILMethodBuilder DefineMethod(string methodName, AccessModifiers modifier, bool earlyBound)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            MethodAttributes attributes = MethodAttributes.ReuseSlot | MethodAttributes.HideBySig;
            if (!earlyBound)
                attributes |= MethodAttributes.Virtual;

            switch (modifier)
            {
                case AccessModifiers.Public:
                    attributes |= MethodAttributes.Public;
                    break;
                case AccessModifiers.Protected:
                    attributes |= MethodAttributes.Family;
                    break;
                case AccessModifiers.ProtectedInternal:
                    attributes |= MethodAttributes.FamORAssem;
                    break;
                case AccessModifiers.Internal:
                    attributes |= MethodAttributes.Assembly;
                    break;
                case AccessModifiers.Private:
                    attributes |= MethodAttributes.Private;
                    break;
            }
            return this.DefineMethod(methodName, attributes);
        }
        public ILMethodBuilder DefineMethod(string methodName, MethodAttributes attributes)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            ILMethodBuilder builder = new ILMethodBuilder(this, methodName, attributes);
            if (this._definedMethods.ContainsKey(methodName))
                this._definedMethods[methodName].Add(builder);
            else
            {
                List<ILMethodBuilder> list = new List<ILMethodBuilder>();
                list.Add(builder);
                this._definedMethods.Add(methodName, list);
            }
            return builder;
        }
        public ILMethodBuilder DefineMethod(string methodName, MethodAttributes attributes, CallingConventions conventions)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            ILMethodBuilder builder = new ILMethodBuilder(this, methodName, attributes, conventions);
            if (this._definedMethods.ContainsKey(methodName))
                this._definedMethods[methodName].Add(builder);
            else
            {
                List<ILMethodBuilder> list = new List<ILMethodBuilder>();
                list.Add(builder);
                this._definedMethods.Add(methodName, list);
            }
            return builder;
        }
        public ILTypeBuilder DefineNestedType(string typeName, TypeAttributes attributes)
        {
            return this.DefineNestedType(typeName, attributes, null, null);
        }
        public ILTypeBuilder DefineNestedType(string typeName, TypeAttributes attributes, Type baseType)
        {
            return this.DefineNestedType(typeName, attributes, baseType, null);
        }
        public ILTypeBuilder DefineNestedType(string typeName, TypeAttributes attributes, Type[] interfaces)
        {
            return this.DefineNestedType(typeName, attributes, null, interfaces);
        }
        public ILTypeBuilder DefineNestedType(string typeName, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            return this.ILAssemblyBuilder.DefineNestedType(this, typeName, attributes, baseType, interfaces);
        }
        public ILMethodBuilder DefineOverrideMethod(MethodInfo baseMethod)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");
            if (baseMethod == null)
                throw new ArgumentNullException("baseMethod");

            Type tempBase = baseMethod.DeclaringType;
            bool isMethodOnAnyBases = tempBase == typeof(object);
            while (!tempBase.IsInterface && tempBase != typeof(object) && !isMethodOnAnyBases)
            {
                isMethodOnAnyBases = this.BaseType == tempBase;
                tempBase = tempBase.BaseType;
            }

            if (!isMethodOnAnyBases && Array.IndexOf(this._interfaces, baseMethod.DeclaringType) == -1)
                throw new BuildException("Temel metot, temel sınıf ya da uygulanan arabirimlerin en az bir tanesinde tanımlanmış olmalıdır.");
            if (baseMethod.IsStatic)
                throw new BuildException("Static metotlar geçersiz kılınamaz.");
            if (baseMethod.IsFinal)
                throw new BuildException("Mühürlenmiş metotlar geçersiz kılınamaz.");
            if (!baseMethod.IsVirtual)
                throw new BuildException("Metodun hükümsüz kılınabilmesi için sanal veya soyut olması gerekiyor.");

            ILMethodBuilder builder = new ILMethodBuilder(this, baseMethod);
            if (this._definedMethods.ContainsKey(baseMethod.Name))
                this._definedMethods[baseMethod.Name].Add(builder);
            else
            {
                List<ILMethodBuilder> list = new List<ILMethodBuilder>();
                list.Add(builder);
                this._definedMethods.Add(baseMethod.Name, list);
            }
            return builder;
        }
        public ILPropertyBuilder DefineOverrideProperty(PropertyInfo baseProperty)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            if (baseProperty.GetGetMethod() == null ? false : baseProperty.GetGetMethod().IsStatic || baseProperty.GetSetMethod() == null ? false : baseProperty.GetSetMethod().IsStatic)
                throw new BuildException("Static özellikler geçersiz kılınamaz.");

            if (this._fields.ContainsKey(baseProperty.Name))
                throw new BuildException(baseProperty.Name + " isimli özellik daha önce tanımlanmış.");

            ILPropertyBuilder builder = new ILPropertyBuilder(this, baseProperty);
            this._properties.Add(baseProperty.Name, builder);
            return builder;
        }
        public ILPropertyBuilder DefineProperty(string propertyName, Type propertyType, bool isStatic)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            if (this._fields.ContainsKey(propertyName))
                throw new DefinitionException(propertyName + " isimli özellik daha önce tanımlanmış.");

            ILPropertyBuilder builder = new ILPropertyBuilder(this, propertyName, propertyType, isStatic);
            this._properties.Add(propertyName, builder);
            return builder;
        }
        public FieldBuilder FindFieldBuilder(string name)
        {
            if (this._isBuild)
                throw new InvalidOperationException("Tip daha önce derlenmiş. Üye aramak, getirmek ve yönetmek için System.Reflection kullanılmalıdır.");

            try { return this._fields[name]; }
            catch (Exception) { return null; }
        }
        public ILMethodBuilder FindMethod(string name, Type[] parameters)
        {
            if (this._isBuild)
                throw new InvalidOperationException("Tip daha önce derlenmiş. Üye aramak, getirmek ve yönetmek için System.Reflection kullanılmalıdır.");

            if (this._definedMethods.ContainsKey(name))
            {
                List<ILMethodBuilder> methodsByName = this._definedMethods[name];
                List<ILMethodBuilder> competibles = new List<ILMethodBuilder>();
                for (int i = 0; i < methodsByName.Count; i++)
                {
                    ILMethodBuilder builder = methodsByName[i];
                    Type[] containedParameters = builder.GetParameterTypes();
                    if (containedParameters.Length == parameters.Length)
                    {
                        bool parameterCompetible = containedParameters.Length == 0; //ArgCount 0 ise, methotlar uyumludur.
                        for (int j = 0; j < containedParameters.Length; j++)
                        {
                            Type contain = containedParameters[j];
                            Type parameter = parameters[j];
                            if (!(parameterCompetible = (contain.IsGenericParameter || contain.IsAssignableFrom(parameter))))
                                break;
                        }
                        if (parameterCompetible)
                            competibles.Add(builder);
                        else
                            continue;
                    }
                    else
                        continue;
                }
                if (competibles.Count == 0)
                    return null;
                else
                {
                    for (int i = 0; i < competibles.Count; i++)
                    {
                        ILMethodBuilder builder = competibles[i];
                        Type[] containedParameters = builder.GetParameterTypes();
                        if (containedParameters.Length == parameters.Length)
                        {
                            bool parameterCompetible = containedParameters.Length == 0; //ArgCount 0 ise, methotlar uyumludur.
                            for (int j = 0; j < containedParameters.Length; j++)
                            {
                                Type contain = containedParameters[j];
                                Type parameter = parameters[j];
                                if (!(parameterCompetible = (contain.IsGenericParameter || contain.IsAssignableFrom(parameter))))
                                    break;
                            }
                            if (parameterCompetible)
                                return builder;
                            else
                                continue;
                        }
                        else
                            continue;
                    }
                    return competibles[0];
                }
            }
            else
                return null;
        }
        public ILPropertyBuilder FindPropertyBuilder(string name)
        {
            if (this._isBuild)
                throw new InvalidOperationException("Tip daha önce derlenmiş. Üye aramak, getirmek ve yönetmek için System.Reflection kullanılmalıdır.");

            try { return this._properties[name]; }
            catch (Exception) { return null; }
        }
        public Type[] GetGenericArgumentTypes()
        {
            if (this._isBuild)
                throw new InvalidOperationException("Tip daha önce derlenmiş. Üye aramak, getirmek ve yönetmek için System.Reflection kullanılmalıdır.");

            Type[] args = new Type[this._genArgs.Count];
            int index = 0;
            foreach (var item in this._genArgs.Values)
            {
                args[index] = item;
                index++;
            }
            return args;
        }
        public Type[] GetInterfaces()
        {
            if (this._interfaces == null)
                return new Type[0];
            else
                return this._interfaces.Clone() as Type[];
        }
        public bool HasField(string name)
        {
            if (this._isBuild)
                throw new InvalidOperationException("Tip daha önce derlenmiş. Üye aramak, getirmek ve yönetmek için System.Reflection kullanılmalıdır.");

            return this._fields.ContainsKey(name);
        }
        public bool HasProperty(string name)
        {
            if (this._isBuild)
                throw new InvalidOperationException("Tip daha önce derlenmiş. Üye aramak, getirmek ve yönetmek için System.Reflection kullanılmalıdır.");

            return this._properties.ContainsKey(name);
        }
        public ILMethodBuilder OverrideGetHashCode()
        {
            Type baseType = this.BaseType;
            if (baseType == null)
                baseType = typeof(object);
            MethodInfo getHashCode = baseType.GetMethod("GetHashCode", Type.EmptyTypes);
            return this.DefineOverrideMethod(getHashCode);
        }
        public ILMethodBuilder OverrideToString()
        {
            Type baseType = this.BaseType;
            if (baseType == null)
                baseType = typeof(object);
            MethodInfo toString = baseType.GetMethod("ToString", Type.EmptyTypes);
            return this.DefineOverrideMethod(toString);
        }
        public void SetBaseType(Type baseType)
        {
            Type runtime = typeof(Type).GetType();

            if (this._setBaseType)
                throw new BuildException("Daha önce bir temel sınıf ataması yapılmış. Birden fazla temel sınıf kullanılamaz.");
            if (baseType.GetType() != runtime)
                throw new ArgumentException("Temel sınıf tür tanımlayıcısı, System.RuntimeType tipinde olmalı.");
            if (baseType.IsAbstract && baseType.IsSealed)
                throw new BuildException("Static sınıflar temel sınıf olarak kullanılamaz.");
            if (baseType.IsSealed)
                throw new BuildException("Mühürlenmiş sınıflar temel sınıf olarak kullanılamaz.");
            if(baseType.IsInterface)
                throw new BuildException("Arabirimler temel sınıf olarak kullanılamaz.");

            this.TypeBuilder.SetParent(baseType);
        }
        public void SetConstraint(string genericArgName, Type baseTypeConstraint, Type[] interfaceConstraints)
        {
            if (this._isBuild)
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");

            GenericTypeParameterBuilder parameterBuilder = this.TypeOfGenericArgument(genericArgName) as GenericTypeParameterBuilder;
            if (baseTypeConstraint != null)
                parameterBuilder.SetBaseTypeConstraint(baseTypeConstraint);
            if (interfaceConstraints != null && interfaceConstraints.Length > 0)
                parameterBuilder.SetInterfaceConstraints(interfaceConstraints);
        }
        public Type TypeOfGenericArgument(string argName)
        {
            if (this._isBuild)
                throw new InvalidOperationException("Tip daha önce derlenmiş. Üye aramak, getirmek ve yönetmek için System.Reflection kullanılmalıdır.");

            if (this._genArgs == null)
                throw new ArgumentException("Bu tip generic olarak yapılandırılmamış..");
            else
            {
                try { return this._genArgs[argName]; }
                catch (ArgumentException) { throw new MemberNotFoundException(argName + " isimli bir generic argüman bulunamadı."); }
            }
        }

        void IBuilder.OnBuild()
        {
            for (int i = 0; i < this._ctors.Count; i++)
                ((IBuilder)this._ctors[i]).OnBuild();

            foreach (KeyValuePair<string, List<ILMethodBuilder>> methodsAsName in this._definedMethods)
            {
                List<ILMethodBuilder> methods = methodsAsName.Value;
                for (int i = 0; i < methods.Count; i++)
                    ((IBuilder)methods[i]).OnBuild();
            }

            this._isBuild = true;

            this._fields.Clear();
            this._fields = null;
            this._ctors.Clear();
            this._ctors = null;
            this._properties.Clear();
            this._properties = null;
            this._genArgs.Clear();
            this._genArgs = null;
            this._interfaces = null;
        }
        bool IBuilder.IsBuild { get { return this._isBuild; } }
    }
}
