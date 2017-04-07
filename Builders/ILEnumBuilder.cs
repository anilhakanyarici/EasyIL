using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILEnumBuilder
    {
        private Type _compiledType;
        private ILAssemblyBuilder _ilAsmBuilder;

        public ILAssemblyBuilder ILAssemblyBuilder { get { return this.ILAssemblyBuilder; } }
        public EnumBuilder EnumBuilder { get; private set; }
        public string Name { get; private set; }

        internal ILEnumBuilder(ILAssemblyBuilder asmBuilder, string name, TypeAttributes attributes)
        {
            this._ilAsmBuilder = asmBuilder;
            this.EnumBuilder = asmBuilder.ModuleBuilder.DefineEnum(name, attributes, typeof(int));
            this.Name = name;
        }

        public Type CompileType()
        {
            if (this._compiledType == null)
                return this._compiledType = this.ILAssemblyBuilder.CompileType(this);
            else
                return this._compiledType;
        }
        public Enum CreateInstance()
        {
            Type type = this.CompileType();
            return Activator.CreateInstance(type) as Enum;
        }
        public Enum CreateInstance(int value)
        {
            Type type = this.CompileType();
            return (Enum)Enum.ToObject(type, value);
        }
        public Enum CreateInstance(string value)
        {
            Type type = this.CompileType();
            return (Enum)Enum.ToObject(type, value);
        }
        public void DefineLiteral(string literalName, int literalValue)
        {
            if (this._compiledType == null)
                this.EnumBuilder.DefineLiteral(literalName, literalValue);
            else
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");
        }
        public void SetFlagsAttribute()
        {
            if (this._compiledType == null)
            {
                CustomAttributeBuilder flagsBuilder = new CustomAttributeBuilder(typeof(FlagsAttribute).GetConstructors()[0], null);
                this.EnumBuilder.SetCustomAttribute(flagsBuilder);
            }
            else
                throw new DefinitionException("Tip daha önce derlenmiş. Derlenmiş tipler üzerinde yeni üye eklemesi yapılamaz.");
        }
    }
}
