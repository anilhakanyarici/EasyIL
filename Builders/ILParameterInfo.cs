using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILParameterInfo
    {
        private Type[] _constraints;

        public ParameterAttributes Attributes { get; internal set; }
        public ParameterBuilder Builder { get; internal set; }
        public Type[] Constraints { get { if (this._constraints == null) this._constraints = new Type[0]; return this._constraints; } internal set { this._constraints = value; } }
        public object OptionalValue { get; internal set; }
        public Type ParameterType { get; internal set; }
        public string ParameterName { get; internal set; }
    }
}
