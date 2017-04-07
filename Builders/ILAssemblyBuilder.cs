using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILAssemblyBuilder
    {
        private Dictionary<string, ILTypeBuilder> _definedTypes;
        private Dictionary<string, ILEnumBuilder> _definedEnums;
        private Dictionary<string, Type> _compiledTypes;
        private bool _isBuild;

        public bool IsBuild { get { return this._isBuild; } }
        public AssemblyBuilder AssemblyBuilder { get; private set; }
        public ModuleBuilder ModuleBuilder { get; private set; }

        public ILAssemblyBuilder(string assemblyName)
            : this(assemblyName, false)
        {

        }
        public ILAssemblyBuilder(string name, bool emitSymbolInfo)
            : this(AppDomain.CurrentDomain, name, emitSymbolInfo)
        {

        }
        public ILAssemblyBuilder(AppDomain domain, string assemblyName)
            : this(domain, assemblyName, false)
        {

        }
        public ILAssemblyBuilder(AppDomain domain, string assemblyName, bool emitSymbolInfo)
        {
            AssemblyName assembly = new AssemblyName(assemblyName);
            this.AssemblyBuilder = domain.DefineDynamicAssembly(assembly, AssemblyBuilderAccess.RunAndSave);
            this.ModuleBuilder = this.AssemblyBuilder.DefineDynamicModule(assembly.Name, assembly.Name + ".dll", emitSymbolInfo);
            this._definedTypes = new Dictionary<string, ILTypeBuilder>();
            this._definedEnums = new Dictionary<string, ILEnumBuilder>();
            this._compiledTypes = new Dictionary<string, Type>();
        }

        public void Build()
        {
            ILEnumBuilder[] definedEnums = new ILEnumBuilder[this._definedEnums.Count];
            this._definedEnums.Values.CopyTo(definedEnums, 0);
            ILTypeBuilder[] definedTypes = new ILTypeBuilder[this._definedTypes.Count];
            this._definedTypes.Values.CopyTo(definedTypes, 0);

            for (int i = 0; i < definedEnums.Length; i++)
                this.CompileType(definedEnums[i]);
            for (int i = 0; i < definedTypes.Length; i++)
                this.CompileType(definedTypes[i]);

            this._isBuild = true;
        }
        public Type CompileType(ILTypeBuilder builder)
        {
            string realName = builder.FullName;
            TypeBuilder realBuilder = builder.TypeBuilder;
            ((IBuilder)builder).OnBuild();
            Type createdType = realBuilder.CreateType();
            this._compiledTypes.Add(realName, createdType);
            this._definedTypes.Remove(realName);
            return createdType;
        }
        public Type CompileType(ILEnumBuilder builder)
        {
            EnumBuilder realBuilder = builder.EnumBuilder;
            Type createdType = realBuilder.CreateType();
            this._compiledTypes.Add(builder.Name, createdType);
            this._definedEnums.Remove(builder.Name);
            return createdType;
        }
        public TCast CreateInstance<TCast>(string typeName, params object[] ctorParameters)
        {
            if (this.HasType(typeName))
            {
                Type demand = this.GetType(typeName);
                return (TCast)Activator.CreateInstance(demand, ctorParameters);
            }
            else if (this.HasILTypeBuilder(typeName))
            {
                ILTypeBuilder demand = this.GetILTypeBuilder(typeName);
                return demand.CreateInstance<TCast>(ctorParameters);
            }
            else
                throw new TypeNotFoundException("Assembly, " + typeName + " isimli türü içermiyor.");
        }
        public Enum CreateEnumObject(string enumName, int value)
        {
            if (this.HasILEnumBuilder(enumName))
            {
                Type demand = this.GetILEnumBuilder(enumName).CompileType();
                return (Enum)Enum.ToObject(demand, value);
            }
            else
                throw new TypeNotFoundException("Assembly, " + enumName + " isimli türü içermiyor.");
        }
        public Enum CreateEnumObject(string enumName, string value)
        {
            if (this.HasILEnumBuilder(enumName))
            {
                Type demand = this.GetILEnumBuilder(enumName).CompileType();
                return (Enum)Enum.ToObject(demand, value);
            }
            else
                throw new TypeNotFoundException("Assembly, " + enumName + " isimli türü içermiyor.");
        }
        public ILEnumBuilder DefineEnum(string enumName, TypeAttributes attributes)
        {
            if (this.IsBuild)
                throw new DefinitionException("Derlenmiş assembly üzerinde yeni tip tanımlanamaz.");

            if (this._definedEnums.ContainsKey(enumName))
                throw new DefinitionException(enumName + " isimli tür daha önce tanımlanmış.");

            if (this._definedTypes.ContainsKey(enumName))
                throw new DefinitionException(enumName + " isimli tür daha önce tanımlanmış.");

            if (this._compiledTypes.ContainsKey(enumName))
                throw new DefinitionException(enumName + " isimli tür daha önce tanımlanmış.");

            ILEnumBuilder builder = new ILEnumBuilder(this, enumName, attributes);
            this._definedEnums.Add(enumName, builder);
            return builder;
        }
        public ILTypeBuilder DefineType(string typeName, TypeAttributes attributes)
        {
            return this.DefineType(typeName, attributes, null, null);
        }
        public ILTypeBuilder DefineType(string typeName, TypeAttributes attributes, Type baseType)
        {
            return this.DefineType(typeName, attributes, baseType, null);
        }
        public ILTypeBuilder DefineType(string typeName, TypeAttributes attributes, Type[] interfaces)
        {
            return this.DefineType(typeName, attributes, null, interfaces);
        }
        public ILTypeBuilder DefineType(string typeName, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            if (this.IsBuild)
                throw new DefinitionException("Derlenmiş assembly üzerinde yeni tip tanımlanamaz.");

            if (this._definedTypes.ContainsKey(typeName))
                throw new DefinitionException(typeName + " isimli tür daha önce tanımlanmış.");

            if (this._definedEnums.ContainsKey(typeName))
                throw new DefinitionException(typeName + " isimli tür daha önce tanımlanmış.");

            if (this._compiledTypes.ContainsKey(typeName))
                throw new DefinitionException(typeName + " isimli tür daha önce tanımlanmış.");

            if (baseType == null)
                baseType = typeof(object);

            if (baseType.Attributes == TypeAttributes.Sealed)
                throw new BuildException("Temel sınıf mühürlü. Mühürlenmiş sınıflardan katılım alınamaz.");
            else if (baseType.IsValueType)
                throw new BuildException("Temel tip bir değer türü. Değer türü yapılarından kalıtım alınamaz.");

            ILTypeBuilder builder = new ILTypeBuilder(this, typeName, attributes, baseType, interfaces);
            this._definedTypes.Add(typeName, builder);
            return builder;
        }
        public ILTypeBuilder DefineNestedType(ILTypeBuilder owner, string typeName, TypeAttributes attributes)
        {
            return this.DefineNestedType(owner, typeName, attributes, null, null);
        }
        public ILTypeBuilder DefineNestedType(ILTypeBuilder owner, string typeName, TypeAttributes attributes, Type baseType)
        {
            return this.DefineNestedType(owner, typeName, attributes, baseType, null);
        }
        public ILTypeBuilder DefineNestedType(ILTypeBuilder owner, string typeName, TypeAttributes attributes, Type[] interfaces)
        {
            return this.DefineNestedType(owner, typeName, attributes, null, interfaces);
        }
        public ILTypeBuilder DefineNestedType(ILTypeBuilder owner, string typeName, TypeAttributes attributes, Type baseType, Type[] interfaces)
        {
            if (this.IsBuild)
                throw new DefinitionException("Derlenmiş assembly üzerinde yeni tip tanımlanamaz.");

            string nestedName = owner.Name + "." + typeName;

            if (this._definedTypes.ContainsKey(nestedName))
                throw new DefinitionException(nestedName + " isimli tür daha önce tanımlanmış.");

            if (this._definedEnums.ContainsKey(nestedName))
                throw new DefinitionException(nestedName + " isimli tür daha önce tanımlanmış.");

            if (this._compiledTypes.ContainsKey(nestedName))
                throw new DefinitionException(nestedName + " isimli tür daha önce tanımlanmış.");

            if (baseType == null)
                baseType = typeof(object);

            if (baseType.Attributes == TypeAttributes.Sealed)
                throw new BuildException("Temel sınıf mühürlü. Mühürlenmiş sınıflardan katılım alınamaz.");
            else if (baseType.IsValueType)
                throw new BuildException("Temel tip bir değer türü. Değer türü yapılarından kalıtım alınamaz.");

            ILTypeBuilder builder = new ILTypeBuilder(owner, typeName, attributes, baseType, interfaces);
            this._definedTypes.Add(nestedName, builder);
            return builder;
        }
        public ILEnumBuilder GetILEnumBuilder(string name)
        {
            if (this._definedEnums.ContainsKey(name))
                return this._definedEnums[name];
            throw new TypeNotFoundException(name + " isimli tür daha önce tanımlanmamış veya derlenmemiş.");
        }
        public ILTypeBuilder GetILTypeBuilder(string name)
        {
            if (this._definedTypes.ContainsKey(name))
                return this._definedTypes[name];
            throw new TypeNotFoundException(name + "isimli tür daha önce tanımlanmamış veya derlenmemiş.");
        }
        public Type GetType(string name)
        {
            if (this._compiledTypes.ContainsKey(name))
                return this._compiledTypes[name];
            else
                return null;
        }
        public bool HasILEnumBuilder(string name)
        {
            return this._definedEnums.ContainsKey(name);
        }
        public bool HasILTypeBuilder(string name)
        {
            return this._definedTypes.ContainsKey(name);
        }
        public bool HasType(string name)
        {
            return this._compiledTypes.ContainsKey(name);
        }
        public void Save(string fileName)
        {
            if (this.IsBuild)
                this.AssemblyBuilder.Save(fileName);
            else
                throw new BuildException("Assembly oluşumunun bir dosyaya aktarılması için önce derlenmesi gerekiyor.");
        }
    }
}
