using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter.Lazies
{
    internal class ILLazyInvoke : ILLazy
    {
        private ILVariable _instance;
        private MethodInfo _method;
        private ILMethodBuilder _methodBuilder;
        private ILData[] _parameters;
        private Type[] _genericArgs;

        public override Type ILType { get { if (this._methodBuilder == null) return this._method.ReturnType; else return this._methodBuilder.ReturnType; } }

        internal ILLazyInvoke(ILCoder coding, ILVariable instance, MethodInfo method, Type[] genericArgs, ILData[] parameters)
            : base(coding)
        {
            this._instance = instance;
            this._method = method;
            this._parameters = parameters;
            this._genericArgs = genericArgs;
            this._methodBuilder = null;
        }
        internal ILLazyInvoke(ILCoder coding, ILVariable instance, ILMethodBuilder methodBuilder, Type[] genericArgs, ILData[] parameters)
            : base(coding)
        {
            this._instance = instance;
            this._methodBuilder = methodBuilder;
            this._parameters = parameters;
            this._genericArgs = genericArgs;
            this._method = null;
        }

        protected override void Push()
        {
            if (this._instance == null)
            {
                if (this._methodBuilder == null)
                    this.invokeStatic();
                else
                    this.invokeStaticBuilder();
            }
            else
            {
                if (this._methodBuilder == null)
                    this.invokeInstance();
                else
                    this.invokeInstanceBuilder();
            }
        }
        protected override void PushAddress()
        {
            this.Push();
        }

        private void invokeInstanceBuilder()
        {
            ILVariable obj = this._instance;
            ILMethodBuilder methodBuilder = this._methodBuilder;
            Type[] genericArgs = this._genericArgs;
            ILData[] invokeParams = this._parameters;

            MethodInfo method = methodBuilder.MethodBuilder;

            if (genericArgs == null || genericArgs.Length == 0)
            {
                if (obj.ILType.IsValueType)
                    ((IILPusher)obj).PushAddress();
                else
                {
                    if (obj.ILType.IsGenericParameter)
                    {
                        ((IILPusher)obj).PushAddress();
                        obj.Coding.Generator.Emit(OpCodes.Constrained, obj.ILType);
                    }
                    else
                        ((IILPusher)obj).Push();
                }

                if (invokeParams != null)
                {
                    Type typeOfObject = typeof(object);
                    Type[] parameterTypes = methodBuilder.GetParameterTypes();
                    for (int i = 0; i < invokeParams.Length; i++)
                    {
                        ILData invokeParam = invokeParams[i];
                        Type parameterType = parameterTypes[i];
                        if (parameterType.IsByRef)
                            ((IILPusher)invokeParam).PushAddress();
                        else
                        {
                            if (parameterType == typeOfObject)
                            {
                                ((IILPusher)invokeParam).Push();
                                obj.Generator.Emit(OpCodes.Box, invokeParam.ILType);
                            }
                            else
                                ((IILPusher)invokeParam).Push();
                        }
                    }
                }

                //if (method.IsVirtual)
                //    obj.Generator.Emit(OpCodes.Callvirt, method);
                //else
                obj.Generator.Emit(OpCodes.Call, method);
            }
            else
            {
                if (obj.ILType.IsValueType)
                    ((IILPusher)obj).PushAddress();
                else
                {
                    if (obj.ILType.IsGenericParameter)
                    {
                        ((IILPusher)obj).PushAddress();
                        obj.Coding.Generator.Emit(OpCodes.Constrained, obj.ILType);
                    }
                    else
                        ((IILPusher)obj).Push();
                }

                if (invokeParams != null)
                {
                    Type typeOfObject = typeof(object);
                    Type[] parameterTypes = methodBuilder.GetParameterTypes();
                    for (int i = 0; i < invokeParams.Length; i++)
                    {
                        ILData invokeParam = invokeParams[i];
                        if (parameterTypes[i] == typeOfObject)
                        {
                            ((IILPusher)invokeParam).Push();
                            obj.Generator.Emit(OpCodes.Box, invokeParam.ILType);
                        }
                        else
                            ((IILPusher)invokeParam).Push();
                    }
                }
                method = method.MakeGenericMethod(genericArgs);
                //if (method.IsVirtual)
                //    obj.Generator.Emit(OpCodes.Callvirt, method);
                //else
                obj.Generator.Emit(OpCodes.Call, method);
            }
        }
        private void invokeInstance()
        {
            ILVariable obj = this._instance;
            MethodInfo method = this._method;
            Type[] genericArgs = this._genericArgs;
            ILData[] invokeParams = this._parameters;

            if (genericArgs == null || genericArgs.Length == 0)
            {
                if (obj.ILType.IsValueType)
                    ((IILPusher)obj).PushAddress();
                else
                {
                    if (obj.ILType.IsGenericParameter)
                    {
                        ((IILPusher)obj).PushAddress();
                        obj.Coding.Generator.Emit(OpCodes.Constrained, obj.ILType);
                    }
                    else
                        ((IILPusher)obj).Push();
                }

                if (invokeParams != null)
                {
                    Type typeOfObject = typeof(object);
                    ParameterInfo[] parameters = method.GetParameters();
                    for (int i = 0; i < invokeParams.Length; i++)
                    {
                        ILData invokeParam = invokeParams[i];
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType.IsByRef)
                            ((IILPusher)invokeParam).PushAddress();
                        else
                        {
                            if (parameters[i].ParameterType == typeOfObject)
                            {
                                ((IILPusher)invokeParam).Push();
                                obj.Generator.Emit(OpCodes.Box, invokeParam.ILType);
                            }
                            else
                                ((IILPusher)invokeParam).Push();
                        }
                       
                    }
                }

                //if (method.IsVirtual)
                //    obj.Generator.Emit(OpCodes.Callvirt, method);
                //else
                obj.Generator.Emit(OpCodes.Call, method);
            }
            else
            {
                if (obj.ILType.IsValueType)
                    ((IILPusher)obj).PushAddress();
                else
                {
                    if (obj.ILType.IsGenericParameter)
                    {
                        ((IILPusher)obj).PushAddress();
                        obj.Coding.Generator.Emit(OpCodes.Constrained, obj.ILType);
                    }
                    else
                        ((IILPusher)obj).Push();
                }

                if (invokeParams != null)
                {
                    Type typeOfObject = typeof(object);
                    ParameterInfo[] parameters = method.GetParameters();
                    for (int i = 0; i < invokeParams.Length; i++)
                    {
                        ILData invokeParam = invokeParams[i];
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType.IsByRef)
                            ((IILPusher)invokeParam).PushAddress();
                        else
                        {
                            if (parameters[i].ParameterType == typeOfObject)
                            {
                                ((IILPusher)invokeParam).Push();
                                obj.Generator.Emit(OpCodes.Box, invokeParam.ILType);
                            }
                            else
                                ((IILPusher)invokeParam).Push();
                        }
                    }
                }
                method = method.MakeGenericMethod(genericArgs);
                obj.Generator.Emit(OpCodes.Call, method);
            }
        }
        private void invokeStaticBuilder()
        {
            ILMethodBuilder staticMethodBuilder = this._methodBuilder;
            Type[] genericArgs = this._genericArgs;
            ILData[] invokeParams = this._parameters;

            if (genericArgs == null || genericArgs.Length == 0)
            {
                if (invokeParams != null)
                {
                    Type typeOfObject = typeof(object);
                    Type[] parameterTypes = staticMethodBuilder.GetParameterTypes();
                    for (int i = 0; i < invokeParams.Length; i++)
                    {
                        ILData invokeParam = invokeParams[i];
                        Type parameterType = parameterTypes[i];
                        if (parameterType.IsByRef)
                            ((IILPusher)invokeParam).PushAddress();
                        else
                        {
                            if (parameterTypes[i] == typeOfObject)
                            {
                                ((IILPusher)invokeParam).Push();
                                this.Generator.Emit(OpCodes.Box, invokeParam.ILType);
                            }
                            else
                                ((IILPusher)invokeParam).Push();
                        }
                    }
                }
                this.Generator.Emit(OpCodes.Call, staticMethodBuilder.MethodBuilder);
            }
            else
            {
                if (invokeParams != null)
                {
                    Type typeOfObject = typeof(object);
                    Type[] parameterTypes = staticMethodBuilder.GetParameterTypes();
                    for (int i = 0; i < invokeParams.Length; i++)
                    {
                        ILData invokeParam = invokeParams[i];
                        Type parameterType = parameterTypes[i];
                        if (parameterType.IsByRef)
                            ((IILPusher)invokeParam).PushAddress();
                        else
                        {
                            if (parameterTypes[i] == typeOfObject)
                            {
                                ((IILPusher)invokeParam).Push();
                                this.Generator.Emit(OpCodes.Box, invokeParam.ILType);
                            }
                            else
                                ((IILPusher)invokeParam).Push();
                        }
                       
                    }
                }
                MethodInfo method = staticMethodBuilder.MethodBuilder.MakeGenericMethod(genericArgs);
                this.Generator.Emit(OpCodes.Call, method);
            }
        }
        private void invokeStatic()
        {
            MethodInfo staticMethod = this._method;
            Type[] genericArgs = this._genericArgs;
            ILData[] invokeParams = this._parameters;

            if (genericArgs == null || genericArgs.Length == 0)
            {
                if (invokeParams != null)
                {
                    Type typeOfObject = typeof(object);
                    ParameterInfo[] parameters = staticMethod.GetParameters();
                    for (int i = 0; i < invokeParams.Length; i++)
                    {
                        ILData invokeParam = invokeParams[i];
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType.IsByRef)
                            ((IILPusher)invokeParam).PushAddress();
                        else
                        {
                            if (parameters[i].ParameterType == typeOfObject)
                            {
                                ((IILPusher)invokeParam).Push();
                                this.Generator.Emit(OpCodes.Box, invokeParam.ILType);
                            }
                            else
                                ((IILPusher)invokeParam).Push();
                        }
                       
                    }
                }
                this.Generator.Emit(OpCodes.Call, staticMethod);
            }
            else
            {
                if (invokeParams != null)
                {
                    Type typeOfObject = typeof(object);
                    ParameterInfo[] parameters = staticMethod.GetParameters();
                    for (int i = 0; i < invokeParams.Length; i++)
                    {
                        ILData invokeParam = invokeParams[i];
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType.IsByRef)
                            ((IILPusher)invokeParam).PushAddress();
                        else
                        {
                            if (parameters[i].ParameterType == typeOfObject)
                            {
                                ((IILPusher)invokeParam).Push();
                                this.Generator.Emit(OpCodes.Box, invokeParam.ILType);
                            }
                            else
                                ((IILPusher)invokeParam).Push();
                        }
                    }
                }
                staticMethod = staticMethod.MakeGenericMethod(genericArgs);
                this.Generator.Emit(OpCodes.Call, staticMethod);
            }
        }
    }
    internal class ILLazyConstructor : ILLazy
    {
        private ConstructorInfo _constructor;
        private ILData[] _parameters;

        public override Type ILType { get { return this._constructor.ReflectedType; } }

        internal ILLazyConstructor(ILCoder coding, ConstructorInfo constructor, ILData[] parameters)
            : base(coding)
        {
            this._constructor = constructor;
            this._parameters = parameters;
        }

        protected override void Push()
        {
            if (this._parameters != null)
                for (int i = 0; i < this._parameters.Length; i++)
                    ((IILPusher)this._parameters[i]).Push();

            this.Generator.Emit(OpCodes.Newobj, this._constructor);
        }
        protected override void PushAddress()
        {
            this.Push();
        }
    }
}
