using System;

namespace NetworkIO.ILEmitter
{
    public class ILEmitterException : Exception
    {
        public ILEmitterException()
        {

        }
        public ILEmitterException(string message)
            : base(message)
        {

        }
    }
    public class TypeConvertException : ILEmitterException
    {
        public TypeConvertException()
        {

        }
        public TypeConvertException(string message)
            : base(message)
        {

        }
    }
    public class TypeNotFoundException : MemberNotFoundException
    {
        public TypeNotFoundException()
        {

        }
        public TypeNotFoundException(string message)
            : base(message)
        {

        }
    }
    public class MethodNotFoundException : MemberNotFoundException
    {
        public MethodNotFoundException()
        {

        }
        public MethodNotFoundException(string message)
            : base(message)
        {

        }
    }
    public class FieldNotFoundException : MemberNotFoundException
    {
        public FieldNotFoundException()
        {

        }
        public FieldNotFoundException(string message)
            : base(message)
        {

        }
    }
    public class MemberNotFoundException : ILEmitterException
    {
        public MemberNotFoundException()
        {

        }
        public MemberNotFoundException(string message)
            : base(message)
        {

        }
    }
    public class DefinitionException : ILEmitterException
    {
        public DefinitionException()
        {

        }
        public DefinitionException(string message)
            : base(message)
        {

        }
    }
    public class BuildException : ILEmitterException
    {
        public BuildException()
        {

        }
        public BuildException(string message)
            : base(message)
        {

        }
    }
    public class CodingException : BuildException
    {
        public CodingException()
        {

        }
        public CodingException(string message)
            : base(message)
        {

        }
    }
}
