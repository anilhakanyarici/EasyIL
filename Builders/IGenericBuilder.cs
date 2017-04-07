using System;

namespace NetworkIO.ILEmitter
{
    public interface IGenericBuilder
    {
        bool ContainsGenericParameterName(string argName);
        Type[] GetGenericArgumentTypes();
        Type TypeOfGenericArgument(string argName);
        void DefineGenericParameters(params string[] argNames);
        void SetConstraint(string genericArgName, Type baseTypeConstraint, Type[] interfaceConstraints);
    }
}
