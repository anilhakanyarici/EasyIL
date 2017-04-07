using System;
using System.Collections.Generic;
using System.Reflection;

namespace NetworkIO.ILEmitter
{
    internal static class ILExtentionUtils
    {
        internal static readonly Type RuntimeType = typeof(Type).GetType();

        internal static MethodInfo OpOverloads(Type declared, Comparisons comparer, Type leftOperandType, Type rightOperandType)
        {
            string methodName = "";
            switch (comparer)
            {
                case Comparisons.NotEqual:
                    methodName = "op_Inequality";
                    break;
                case Comparisons.Equal:
                    methodName = ":op_Equality";
                    break;
                case Comparisons.Greater:
                    methodName = "op_GreaterThan";
                    break;
                case Comparisons.Less:
                    methodName = "op_LessThan";
                    break;
                case Comparisons.GreatOrEqual:
                    methodName = "op_GreaterThanOrEqual";
                    break;
                case Comparisons.LessOrEqual:
                    methodName = "op_LessThanOrEqual";
                    break;
                default:
                    break;
            }
            return declared.GetMethod(methodName, new Type[] { leftOperandType, rightOperandType });
        }
        internal static MethodInfo OpOverloads(Type declared, DoubleOperators doubleOperator, Type leftOperandType, Type rightOperandType)
        {
            string methodName = "";
            switch (doubleOperator)
            {
                case DoubleOperators.Add:
                    methodName = "op_Addition";
                    break;
                case DoubleOperators.Sub:
                    methodName = "op_Subtraction";
                    break;
                case DoubleOperators.Mul:
                    methodName = "op_Multiply";
                    break;
                case DoubleOperators.Div:
                    methodName = "op_Division";
                    break;
                case DoubleOperators.Rem:
                    methodName = "op_Modulus";
                    break;
                case DoubleOperators.And:
                    methodName = "op_BitwiseAnd";
                    break;
                case DoubleOperators.Or:
                    methodName = "op_BitwiseOr";
                    break;
                case DoubleOperators.Xor:
                    methodName = "op_ExclusiveOr";
                    break;
                case DoubleOperators.LShift:
                    methodName = "op_LeftShift";
                    break;
                case DoubleOperators.RShift:
                    methodName = "op_RightShift";
                    break;
                default:
                    break;
            }
            return declared.GetMethod(methodName, new Type[] { leftOperandType, rightOperandType });
        }
        internal static MethodInfo OpOverloads(Type declared, SingleOperators singleOperator, Type operandType)
        {
            string methodName = "";
            switch (singleOperator)
            {
                case SingleOperators.Not:
                    methodName = "op_OnesComplement";
                    break;
                case SingleOperators.Neg:
                    methodName = "op_UnaryNegation";
                    break;
                case SingleOperators.Increment:
                    methodName = "op_Increment";
                    break;
                case SingleOperators.Decrement:
                    methodName = "op_Decrement";
                    break;
                case SingleOperators.Plus:
                    methodName = "op_UnaryPlus";
                    break;
            }
            return declared.GetMethod(methodName, new Type[] { operandType });
        }
        internal static MethodInfo OpOverloadsImplicit(Type declared, Type parameterType)
        {
            return declared.GetMethod("op_Implicit", new Type[] { parameterType });
        }
        internal static MethodInfo OpOverloadsExplicit(Type declared, Type parameterType)
        {
            return declared.GetMethod("op_Explicit", new Type[] { parameterType });
        }
        internal static Type ComparePrimitiveNumberAssignation(Type left, Type right)
        {
            if (left == typeof(ulong) && (right == typeof(float) || right == typeof(double)))
                return right;
            else if (right == typeof(ulong) && (left == typeof(float) || left == typeof(double)))
                return left;
            else
            {
                if (left == typeof(ulong) && right != typeof(ulong))
                    if (right == typeof(byte) || right == typeof(ushort) || right == typeof(uint))
                        return typeof(ulong);
                    else if (right == typeof(double))
                        return typeof(double);
                    else if (right == typeof(float))
                        return typeof(float);
                    else
                        return null;
                else if (left != typeof(ulong) && right == typeof(ulong))
                    if (left == typeof(byte) || left == typeof(ushort) || left == typeof(uint))
                        return typeof(ulong);
                    else if (left == typeof(double))
                        return typeof(double);
                    else if (left == typeof(float))
                        return typeof(float);
                    else
                        return null;
                else if (left == typeof(ulong) && right == typeof(ulong))
                    return typeof(ulong);
                else
                {
                    if (left == right)
                        return left;
                    else
                    {
                        if (left == typeof(long) || right != typeof(long))
                            return left;
                        else if ((left == typeof(uint) && right == typeof(int)) || (left == typeof(int) && right == typeof(uint)))
                            return typeof(long);
                        else if ((left == typeof(float) && right != typeof(float)) || (left != typeof(float) && right == typeof(float)))
                        {
                            if (left == typeof(double) || right == typeof(double))
                                return typeof(double);
                            else
                                return typeof(float);
                        }
                        else
                            return typeof(double);
                    }
                }
            }
        }
        internal static string Signature(string methodName, Type[] parameters)
        {
            string signature = "";
            signature += methodName += "(";
            if (parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length - 1; i++)
                    signature += parameters[i].ToString() + ", ";
                signature += parameters[parameters.Length - 1].ToString();
            }
            return signature + ")";
        }
        internal static string Signature(MethodInfo info) //İmzalama hatalı olursa diye yazıldı.
        {
            ParameterInfo[] parameters = info.GetParameters();
            Type[] parameterType = new Type[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                parameterType[i] = parameters[i].ParameterType;
            return ILExtentionUtils.Signature(info.Name, parameterType);
        }
        internal static Type[] ParametersToTypeList(ILData[] invokeParameters)
        {
            if (invokeParameters == null || invokeParameters.Length == 0)
                return new Type[0];
            else
            {
                Type[] types = new Type[invokeParameters.Length];
                for (int i = 0; i < types.Length; i++)
                    types[i] = invokeParameters[i].ILType;
                return types;
            }
        }
        internal static ConstructorInfo FindConstructor(Type declaredType, Type[] parameterTypes)
        {
            ConstructorInfo[] ctors = declaredType.GetConstructors();
            for (int i = 0; i < ctors.Length; i++)
            {
                ConstructorInfo info = ctors[i];
                ParameterInfo[] ctorParameters = info.GetParameters();
                bool isCompetible = false;
                if (ctorParameters.Length == parameterTypes.Length)
                {
                    isCompetible = parameterTypes.Length == 0;
                    for (int j = 0; j < ctorParameters.Length; j++)
                    {
                        Type parameterType = parameterTypes[j];
                        Type ctorParam = ctorParameters[j].ParameterType;
                        if (isCompetible = ctorParam.IsGenericParameter || ctorParam.IsAssignableFrom(parameterType))
                            break; //j
                    }
                    if (isCompetible)
                        return info;
                    else
                        continue; //i
                }
                else
                    continue; //i
            }
            return null;
        }
        internal static MethodInfo FindMethod(Type declaredType, string methodName, Type[] parameterTypes, bool isStatic)
        {
            MethodInfo[] methods = declaredType.GetMethods((isStatic ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.Public | BindingFlags.NonPublic);
            List<MethodInfo> competibleMethods = new List<MethodInfo>();

            if (methods.Length == 0)
                return null;
            else
            {
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != methodName)
                        continue;

                    ParameterInfo[] containedParameters = method.GetParameters();
                    if (containedParameters.Length == parameterTypes.Length)
                    {
                        bool parameterCompetible = containedParameters.Length == 0; //ArgCount 0 ise, methotlar uyumludur.
                        for (int j = 0; j < containedParameters.Length; j++)
                        {
                            Type contain = containedParameters[j].ParameterType;
                            Type parameter = parameterTypes[j];
                            if (!(parameterCompetible = (contain.IsGenericParameter || contain.IsAssignableFrom(parameter))))
                                break;
                        }
                        if (parameterCompetible)
                            competibleMethods.Add(method);
                        else
                            continue;
                    }
                }
                if (competibleMethods.Count == 0)
                    return null;
                else
                {
                    for (int i = 0; i < competibleMethods.Count; i++)
                    {
                        MethodInfo method = competibleMethods[i];
                        ParameterInfo[] containedParameters = method.GetParameters();
                        if (containedParameters.Length == parameterTypes.Length)
                        {
                            bool parameterCompetible = containedParameters.Length == 0; //ArgCount 0 ise, methotlar uyumludur.
                            for (int j = 0; j < containedParameters.Length; j++)
                            {
                                Type contain = containedParameters[j].ParameterType;
                                Type parameter = parameterTypes[j];
                                if (!(parameterCompetible = (contain.IsGenericParameter || contain == parameter)))
                                    break;
                            }
                            if (parameterCompetible)
                                return method;
                            else
                                continue;
                        }
                    }
                    return competibleMethods[0];
                }
            }
        }
        public static bool IsPrimitive(Type type)
        {
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.String:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.Decimal:
                default:
                    return false;
            }
        }
        public static bool IsPrimitiveNumberType(Type type)
        {
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                case TypeCode.Object:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.String:
                case TypeCode.Decimal:
                default:
                    return false;
            }
        }
    }
}
