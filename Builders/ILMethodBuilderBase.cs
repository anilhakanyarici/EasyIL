using System;
using System.Reflection;

namespace NetworkIO.ILEmitter
{
    public enum ILMethodType { Constructor, Dynamic, Normal, Custom }
    public abstract class ILMethodBuilderBase
    {
        public abstract int ArgumentsCount { get; }
        public abstract MethodAttributes Attributes { get; }
        public virtual ILTypeBuilder ILTypeBuilder { get; protected set; }
        public abstract bool IsStatic { get; }
        public abstract Type ReturnType { get; }
        public abstract ILMethodType MethodType { get; }

        public void AddParameter(Type type, string name)
        {
            this.AddParameter(type, name, ParameterAttributes.None, null);
        }
        public void AddParameter(Type type, string name, ParameterAttributes attributes)
        {
            this.AddParameter(type, name, attributes, null);
        }
        public abstract void AddParameter(Type type, string name, ParameterAttributes attributes, object optionalValue);
        public abstract ILCoder GetCoder();
        public abstract ILParameterInfo GetParameterInfo(int parameterIndex);
        public abstract Type GetParameterType(int parameterIndex);
        public abstract Type[] GetParameterTypes();
        public abstract int ParameterNameToIndex(string parameterName);
    }
}
