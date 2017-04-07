using NetworkIO.ILEmitter.Lazies;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public enum Comparisons { NotEqual, Equal, Greater, Less, GreatOrEqual, LessOrEqual }
    public enum CastOperations { Box, Unbox, UnboxAny, CastClass }
    public enum DoubleOperators { Add, Sub, Mul, Div, Rem, And, Or, Xor, LShift, RShift }
    public enum SingleOperators { Not, Neg, Plus, Increment, Decrement }

    public class ILCoder : IBuilder
    {
        private static MethodInfo instanceActivator = typeof(Activator).GetMethod("CreateInstance", new Type[] { typeof(Type), typeof(object[]) });
        private static MethodInfo objectEquals = typeof(object).GetMethod("Equals", new Type[] { typeof(object), typeof(object) });
        private static MethodInfo typeFromHandle = typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) });
        private static MethodInfo writeLine = typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) });
        private static string disposeMessage = "Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.";

        private List<ILBlocks> _openedBlocks;
        private Dictionary<string, Label> _labels;
        private List<Label> _tryFinallyLabels;
        private Label _returnLabel;
        private bool _isBuild;
        private ILMethodBuilderBase _currentMethod;
        private ILGenerator _generator;
        private LocalBuilder _returnValue;

        public ILArgument Arg0 { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return this.Argument(0); } }
        public ILBase Base { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); if (this.CurrentMethod.IsStatic) throw new CodingException("Base işaretçisi statik metotlar için kullanılamaz."); return new ILBase(this); } }
        public ILIfStatement CurrentIfBlock { get; internal set; }
        public ILMethodBuilderBase CurrentMethod { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return this._currentMethod; } }
        public ILTry CurrentTryBlock { get; internal set; }
        public ILConstant EmptyString { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return new ILConstant(this, ""); } }
        public ILConstant False { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return new ILConstant(this, false); } }
        public ILGenerator Generator { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return this._generator; } }
        public bool IsVoid { get { return this._returnValue == null; } }
        public ILNull Null { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return new ILNull(this, typeof(object)); } }
        public ILConstant One { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return new ILConstant(this, 1); } }
        public ILThis This { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); if (this.CurrentMethod.IsStatic) throw new CodingException("This işaretçisi statik metotlar için kullanılamaz."); return new ILThis(this); } }
        public ILConstant True { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return new ILConstant(this, true); } }
        public ILConstant Zero { get { if (this._isBuild) throw new CodingException(ILCoder.disposeMessage); return new ILConstant(this, 0); } }

        internal ILCoder(ILMethodBuilderBase pinnedMethod, ILGenerator generator)
        {
            this._labels = new Dictionary<string, Label>();
            this._returnLabel = generator.DefineLabel();
            this._currentMethod = pinnedMethod;
            this._generator = generator;
            this._tryFinallyLabels = new List<Label>();
            this._openedBlocks = new List<ILBlocks>();

            if (pinnedMethod.ReturnType != typeof(void))
                this._returnValue = generator.DeclareLocal(pinnedMethod.ReturnType);
        }

        public ILArgument Argument(int argIndex)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            return new ILArgument(this, argIndex, this.CurrentMethod.GetParameterType(argIndex));
        }
        public ILArgument Argument(string argName)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            int paramIndex = this.CurrentMethod.ParameterNameToIndex(argName);
            return new ILArgument(this, paramIndex, this.CurrentMethod.GetParameterType(paramIndex));
        }
        public ILArgument[] Arguments()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            ILArgument[] args = new ILArgument[this.CurrentMethod.ArgumentsCount];
            for (int i = 0; i < args.Length; i++)
                args[i] = this.Argument(i);
            return args;
        }
        public ILLocal ArgumentsToILArray()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            ILArgument[] args = this.Arguments();
            ILLocal array = this.NewArray(typeof(object), args.Length);
            for (int i = 0; i < args.Length; i++)
            {
                ILArgument arg = args[i];
                if (!arg.IsOut)
                    array.StoreElement(i, arg);
            }
            return array;
        }
        public void BaseConstruct(ConstructorInfo constructor, ILData[] parameters)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            if (this.CurrentMethod.MethodType != ILMethodType.Constructor)
                throw new CodingException("Bu metot yalnızca kurucular içerisinde temel bir kurucu çağırmak için kullanılabilir.");

            this.Generator.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < parameters.Length; i++)
            {
                ILData parameter = parameters[i];
                ((IILPusher)parameter).Push();
            }
            this.Generator.Emit(OpCodes.Call, constructor);
        }
        public ILCodeBlock BeginCodeBlock()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            ILCodeBlock block = new ILCodeBlock(this);
            this._openedBlocks.Add(block);
            return block;
        }
        public ILLazy Box(ILData value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (value == null)
                throw new ArgumentNullException("value");
            return new ILLazyCasting(this, value, CastOperations.Box, null);
        }
        public ILArgument[] ByRefArguments()
        {
            List<ILArgument> byrefargs = new List<ILArgument>();
            ILArgument[] arguments = this.Arguments();
            for (int i = 0; i < arguments.Length; i++)
            {
                ILArgument arg = arguments[i];
                if (arg.IsOut || arg.IsRef)
                    byrefargs.Add(arg);
            }
            return byrefargs.ToArray();
        }
        public ILLazy Call(Type declaredType, string methodName, Type[] genericArgs, ILData[] invokeParams)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (methodName == null)
                throw new ArgumentNullException("MethodName");
            if (declaredType == null)
                throw new ArgumentNullException("DeclaredType");

            if (declaredType.GetType() != typeof(Type).GetType())
                throw new ArgumentException("DeclaredType parametresinin türü System.RuntimeType olmalı.");

            Type[] parameterTypes = ILExtentionUtils.ParametersToTypeList(invokeParams);
            MethodInfo info = ILExtentionUtils.FindMethod(declaredType, methodName, parameterTypes, true);
            if (info == null)
                throw new MethodNotFoundException("Tanımlanan tip, çağrılmak istenen yöntemi içermiyor veya yöntem statik değil.");
            else
            {
                if (info.IsGenericMethod && (genericArgs == null || genericArgs.Length == 0))
                    throw new ArgumentNullException("genericArgs", "Çağrılan yöntem bir generic tanımlama içeriyor.");
                else
                {
                    ILLazy push = new ILLazyInvoke(this, null, info, genericArgs, invokeParams);
                    if (push.ILType == typeof(void))
                    {
                        ((IILPusher)push).Push();
                        return null;
                    }
                    else
                        return push;
                }
            }
        }
        public ILLazy Call(string methodName, Type[] genericArgs, ILData[] invokeParams)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            if (this.CurrentMethod.MethodType == ILMethodType.Dynamic)
                throw new NotSupportedException("Bu yöntem dinamik metot içerisinde kullanılamaz. ILLazy ILCoder.Call(Type, string, Type[], ILData[]) methodu kullanılmalıdır.");

            if (methodName == null)
                throw new ArgumentNullException("MethodName");

            Type[] parameterTypes = ILExtentionUtils.ParametersToTypeList(invokeParams);
            ILMethodBuilder methodBuilder = this.CurrentMethod.ILTypeBuilder.FindMethod(methodName, parameterTypes);
            if (methodBuilder == null || !methodBuilder.IsStatic)
                throw new MethodNotFoundException("Tanımlanan tip, çağrılmak istenen yöntemi içermiyor veya yöntem statik değil.");
            else
            {
                if (methodBuilder.MethodBuilder.IsGenericMethod && (genericArgs == null || genericArgs.Length == 0))
                    throw new ArgumentNullException("genericArgs", "Çağrılan yöntem bir generic tanımlama içeriyor.");
                else
                {
                    ILLazy push = new ILLazyInvoke(this, null, methodBuilder, genericArgs, invokeParams);
                    if (push.ILType == typeof(void))
                    {
                        ((IILPusher)push).Push();
                        return null;
                    }
                    else
                        return push;
                }
            }
        }
        public ILLazy Cast(CastOperations operation, ILData value, Type related)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (value == null)
                throw new ArgumentNullException("value");
            else if (related == null)
                throw new ArgumentNullException("related");
            else if (operation == CastOperations.Box && (related != null || related != typeof(object)))
                throw new InvalidCastException("Kutulama işlemi için ilişkili tip System.Object veya boş olmalıdır.");
            else if (operation == CastOperations.Unbox && (related == null ? true : !related.IsValueType))
                throw new InvalidCastException("Kutulaması kaldırılacak nesnenin türü bir referans tür olamaz ve tür boş geçilemez.");
            else if (operation == CastOperations.CastClass && (related == null ? true : related.IsValueType || value.ILType.IsValueType))
                throw new InvalidCastException("Cast işlemi için girilen obje bir değer türü olamaz ve tür boş geçilemez.");
            else
                return new ILLazyCasting(this, value, operation, related);
        }
        public ILLazy Comparison(ILData leftOperand, Comparisons comparer, ILData rightOperand)
        {
            if (leftOperand == null)
                throw new ArgumentNullException("LeftOperand cannot be null.");
            if (rightOperand == null)
                throw new ArgumentNullException("RigthOperand cannot be null.");

            MethodInfo comparerOverload = ILExtentionUtils.OpOverloads(leftOperand.ILType, comparer, leftOperand.ILType, rightOperand.ILType);
            if (comparerOverload == null)
            {
                comparerOverload = ILExtentionUtils.OpOverloads(rightOperand.ILType, comparer, leftOperand.ILType, rightOperand.ILType);
                if (comparerOverload == null)
                {
                    if (ILExtentionUtils.IsPrimitiveNumberType(leftOperand.ILType) && ILExtentionUtils.IsPrimitiveNumberType(leftOperand.ILType))
                        return new ILLazyPrimitiveComparer(this, leftOperand, comparer, rightOperand);
                    else
                        throw new InvalidOperationException("These objects are not comparable.");
                }
                else
                    return new ILLazyInvoke(this, null, comparerOverload, null, new ILData[] { leftOperand, rightOperand });
            }
            else
                return new ILLazyInvoke(this, null, comparerOverload, null, new ILData[] { leftOperand, rightOperand });
        }
        public ILConstant Constant(int value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            return new ILConstant(this, value);
        }
        public ILConstant Constant(long value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            return new ILConstant(this, value);
        }
        public ILConstant Constant(float value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            return new ILConstant(this, value);
        }
        public ILConstant Constant(double value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            return new ILConstant(this, value);
        }
        public ILConstant Constant(string value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (value == null)
                throw new ArgumentNullException("Value cannot be null.");

            return new ILConstant(this, value);
        }
        public void Decrease(ILVariable ilValue)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            ILLazy push = this.Operate(ilValue, DoubleOperators.Add, this.Constant(-1));
            ilValue.AssignFrom(push);
        }
        public ILData Default(Type type)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (type == null)
                throw new ArgumentNullException("Type cannot be null.");

            if (ILExtentionUtils.IsPrimitiveNumberType(type) || type == typeof(decimal))
                return this.Zero;
            else if (type.IsClass)
                return this.Null;
            else
                return this.LazyConstruct(type.GetConstructor(Type.EmptyTypes), null);
        }
        public Label DefineLabel()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            return this.Generator.DefineLabel();
        }
        public void DefineLabel(string labelName)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (labelName == null)
                throw new ArgumentNullException("LabelName cannot be null.");

            if (this._labels.ContainsKey(labelName))
                throw new ArgumentException(labelName + " label defined before.");
            else
            {
                Label label = this.Generator.DefineLabel();
                this._labels.Add(labelName, label);
            }
        }
        public ILDoWhile Do()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            ILDoWhile block = new ILDoWhile(this);
            this._openedBlocks.Add(block);
            return block;
        }
        public ILLazy Equals(ILData objA, ILData objB)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (objA.ILType != typeof(object))
                objA = this.Box(objA);
            if (objB.ILType != typeof(object))
                objB = this.Box(objB);

            return this.Invoke(ILCoder.objectEquals, null, new ILData[] { objA, objB });
        }
        public ILFor For(ILData length)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (length == null)
                throw new ArgumentNullException("length");

            if (ILExtentionUtils.IsPrimitiveNumberType(length.ILType))
            {
                ILLocal i = this.Constant(0).ToLocal(length.ILType);
                ILLazy comparer = this.Comparison(i, Comparisons.Less, length);
                ILLazy operating = this.Operate(SingleOperators.Increment, i);
                return this.For(i, comparer, operating);
            }
            else
                throw new InvalidOperationException("Uzunluğun türü bir primitif sayı türü olmalıdır.");
        }
        public ILFor For(ILLocal i, ILLazy comparer, ILLazy assignToI)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (i == null)
                throw new ArgumentNullException("i");
            if (comparer == null)
                throw new ArgumentNullException("comparer");
            if (assignToI == null)
                throw new ArgumentNullException("assignToI");

            if (i.ILType == assignToI.ILType || i.ILType.IsAssignableFrom(assignToI.ILType))
            {
                ILFor block = new ILFor(this, i, comparer, assignToI);
                this._openedBlocks.Add(block);
                return block;
            }
            else
                throw new TypeConvertException("Yerel i değişkeninin türü ile atanacak olan assignToI değişkenin türü aynı olmalıdır.");
        }
        public ILForeach Foreach(ILVariable enumerable)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (typeof(IEnumerable).IsAssignableFrom(enumerable.ILType))
            {
                ILForeach block = new ILForeach(this, enumerable);
                this._openedBlocks.Add(block);
                return block;
            }
            else
                throw new InvalidOperationException("Girilen değerin tipi, IEnumerable arabirimini içermeli.");
        }
        public ILLocal GenericArgumentsToILArray()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.CurrentMethod.MethodType == ILMethodType.Constructor || this.CurrentMethod.MethodType == ILMethodType.Dynamic)
                throw new NotSupportedException("Bu yöntem dinamik metotlar ve kurucular için desteklenmiyor.");
            else
            {
                ILMethodBuilder builder = this.CurrentMethod as ILMethodBuilder;
                Type[] genericArgs = builder.GetGenericArgumentTypes();
                ILLocal array = this.NewArray(typeof(Type), genericArgs.Length);
                for (int i = 0; i < genericArgs.Length; i++)
                    array.StoreElement(i, this.TypeOf(genericArgs[i]));
                return array;
            }
        }
        public ILField GetStaticField(string fieldName)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (fieldName == null)
                throw new ArgumentNullException("fieldName");
            if (this.CurrentMethod.MethodType == ILMethodType.Dynamic)
                throw new NotSupportedException("Bu yöntem dinamik metot içerisinde kullanılamaz. ILField ILCoder.GetStaticField(Type, string) methodu kullanılmalıdır.");

            FieldInfo field = this.CurrentMethod.ILTypeBuilder.FindFieldBuilder(fieldName);
            if (field == null)
                throw new FieldNotFoundException("Hedef tip, çağrılmak istenen alanı içermiyor.");
            if (field.IsStatic)
                return new ILField(this, field, null);
            else
                throw new FieldNotFoundException("İlgili alan statik değildi.");
        }
        public ILField GetStaticField(Type declaredType, string staticFieldName)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (declaredType == null)
                throw new ArgumentNullException("declaredType");
            if (staticFieldName == null)
                throw new ArgumentNullException("staticFieldName");

            if (declaredType.GetType() != typeof(Type).GetType())
                throw new ArgumentException("DeclaredType parametresinin türü System.RuntimeType olmalı.");

            FieldInfo info = declaredType.GetField(staticFieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (info == null)
                throw new FieldNotFoundException("Hedef tip, çağrılmak istenen alanı içermiyor.");
            else if (info.IsStatic)
                return new ILField(this, info, null);
            else
                throw new FieldNotFoundException("İlgili alan statik değildi.");
        }
        public ILProperty GetStaticProperty(string propertyName)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (propertyName == null)
                throw new ArgumentNullException("propertyName");
            if (this.CurrentMethod.MethodType == ILMethodType.Dynamic)
                throw new NotSupportedException("Bu yöntem dinamik metot içerisinde kullanılamaz. ILProperty ILCoder.GetStaticProperty(Type, string) methodu kullanılmalıdır.");

            ILPropertyBuilder builder = this.CurrentMethod.ILTypeBuilder.FindPropertyBuilder(propertyName);
            if (builder == null)
                throw new MemberNotFoundException("Hedef tip, çağrılmak istenen özelliği içermiyor.");
            if (builder.IsStatic)
                return new ILProperty(this, null, builder);
            else
                throw new MemberNotFoundException("İlgili özellik statik değildi.");

        }
        public ILProperty GetStaticProperty(Type declaredType, string staticPropertyName)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (declaredType == null)
                throw new ArgumentNullException("declaredType");
            if (staticPropertyName == null)
                throw new ArgumentNullException("staticPropertyName");
            if (declaredType.GetType() != typeof(Type).GetType())
                throw new ArgumentException("DeclaredType parametresinin türü System.RuntimeType olmalı.");

            PropertyInfo info = declaredType.GetProperty(staticPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (info == null)
                throw new MemberNotFoundException("Hedef tip, çağrılmak istenen özelliği içermiyor.");
            else if (info.GetGetMethod() == null ? false : info.GetGetMethod().IsStatic || info.GetSetMethod() == null ? false : info.GetSetMethod().IsStatic)
                return new ILProperty(this, null, info);
            else
                throw new MemberNotFoundException("İlgili özellik statik değildi.");
        }
        public void GoTo(Label label)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            this.Generator.Emit(OpCodes.Br_S, label);
        }
        public void GoTo(string labelName)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this._labels.ContainsKey(labelName))
            {
                Label label = this._labels[labelName];
                this.Generator.Emit(OpCodes.Br_S, label);
            }
            else
                throw new ArgumentException(labelName + " label was not defined.");
        }
        public void GoToIfFalse(Label label, ILData ilValue)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            ((IILPusher)ilValue).Push();
            this.Generator.Emit(OpCodes.Brfalse_S, label);
        }
        public void GoToIfFalse(string labelName, ILData ilValue)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this._labels.ContainsKey(labelName))
            {
                Label label = this._labels[labelName];
                ((IILPusher)ilValue).Push();
                this.Generator.Emit(OpCodes.Brfalse_S, label);
            }
            else
                throw new ArgumentException(labelName + " label was not defined.");
        }
        public void GoToIfTrue(Label label, ILData ilValue)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            ((IILPusher)ilValue).Push();
            this.Generator.Emit(OpCodes.Brtrue_S, label);
        }
        public void GoToIfTrue(string labelName, ILData ilValue)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this._labels.ContainsKey(labelName))
            {
                Label label = this._labels[labelName];
                ((IILPusher)ilValue).Push();
                this.Generator.Emit(OpCodes.Brtrue_S, label);
            }
            else
                throw new ArgumentException(labelName + " label was not defined.");
        }
        public ILIfStatement If(ILData comparer)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (comparer == null)
                throw new ArgumentNullException("Comparer cannot be null.");

            if (comparer.ILType == typeof(bool))
            {
                ILIfStatement block = new ILIfStatement(this, comparer);
                this._openedBlocks.Add(block);
                return block;
            }
            else
                throw new InvalidOperationException("Comparer type must be boolean.");
        }
        public void Increase(ILVariable ilValue)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            ILLazy push = this.Operate(ilValue, DoubleOperators.Add, this.One);
            ilValue.AssignFrom(push);
        }
        public ILLazy Invoke(MethodInfo staticMethod, Type[] genericArgs, ILData[] invokeParams)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (staticMethod == null)
                throw new ArgumentNullException("staticMethod");
            if (staticMethod.IsGenericMethod && (genericArgs == null || genericArgs.Length == 0))
                throw new ArgumentNullException("genericArgs", "Çağrılan yöntem bir generic tanımlama içeriyor.");

            if (staticMethod.IsStatic)
            {
                ILLazy push = new ILLazyInvoke(this, null, staticMethod, genericArgs, invokeParams);
                if (push.ILType == typeof(void))
                {
                    ((IILPusher)push).Push();
                    return null;
                }
                else
                    return push;
            }
            else
                throw new InvalidOperationException("Hedef yöntem statik değildi.");
        }
        public ILLazy Invoke(ILMethodBuilder staticMethodBuilder, Type[] genericArgs, ILData[] invokeParams)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.CurrentMethod.MethodType == ILMethodType.Dynamic)
                throw new NotSupportedException("Bu yöntem dinamik metotlar için desteklenmiyor. İnşa esnasındaki bir metot, bir dinamik metot tarafından çağrılamaz.");
            if (staticMethodBuilder == null)
                throw new ArgumentNullException("staticMethodBuilder");
            if (staticMethodBuilder.MethodBuilder.IsGenericMethod && (genericArgs == null || genericArgs.Length == 0))
                throw new ArgumentNullException("genericArgs", "Çağrılan yöntem bir generic tanımlama içeriyor.");

            if (staticMethodBuilder.IsStatic)
            {
                ILLazy push = new ILLazyInvoke(this, null, staticMethodBuilder, genericArgs, invokeParams);
                if (push.ILType == typeof(void))
                {
                    ((IILPusher)push).Push();
                    return null;
                }
                else return push;
            }
            else
                throw new InvalidOperationException("Hedef yöntem statik değildi.");
        }
        public ILLazy LazyConstruct(ConstructorInfo constructor, ILData[] parameters)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (constructor == null)
                throw new ArgumentNullException("constructor");

            return new ILLazyConstructor(this, constructor, parameters);
        }
        public ILLazy LazyConstruct(Type objType, ILData[] parameters)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (objType == null)
                throw new ArgumentNullException("objType");

            Type[] parameterTypes = ILExtentionUtils.ParametersToTypeList(parameters);
            ConstructorInfo ctor = ILExtentionUtils.FindConstructor(objType, parameterTypes);

            if (ctor == null)
                throw new MethodNotFoundException("Tip, bu parametrelere sahip bir kurucu içermiyor.");

            return new ILLazyConstructor(this, ctor, parameters);
        }
        public void MarkLabel(Label label)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            this.Generator.MarkLabel(label);
        }
        public void MarkLabel(string labelName)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (labelName == null)
                throw new ArgumentNullException("labelName");

            if (this._labels.ContainsKey(labelName))
            {
                Label label = this._labels[labelName];
                this.Generator.MarkLabel(label);
            }
            else
                throw new CodingException(labelName + " isminde bir etiket tanımlanmamış. İşaretleme yapılamaz.");
        }
        public ILLocal NewArray(params int[] values)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (values == null)
                throw new ArgumentNullException("values");

            LocalBuilder local = this.Generator.DeclareLocal(typeof(int[]));
            this.Generator.Emit(OpCodes.Ldc_I4, values.Length);
            this.Generator.Emit(OpCodes.Newarr, typeof(int));
            this.Generator.Emit(OpCodes.Stloc, local);

            for (int i = 0; i < values.Length; i++)
            {
                this.Generator.Emit(OpCodes.Ldloc, local);
                this.Generator.Emit(OpCodes.Ldc_I4, i);
                this.Generator.Emit(OpCodes.Ldc_I4, values[i]);
                this.Generator.Emit(OpCodes.Stelem_I4);
            }
            return new ILLocal(this, local);
        }
        public ILLocal NewArray(params string[] values)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (values == null)
                throw new ArgumentNullException("values");

            LocalBuilder local = this.Generator.DeclareLocal(typeof(string[]));
            this.Generator.Emit(OpCodes.Ldc_I4, values.Length);
            this.Generator.Emit(OpCodes.Newarr, typeof(string));
            this.Generator.Emit(OpCodes.Stloc, local);

            for (int i = 0; i < values.Length; i++)
            {
                this.Generator.Emit(OpCodes.Ldloc, local);
                this.Generator.Emit(OpCodes.Ldc_I4, i);
                this.Generator.Emit(OpCodes.Ldstr, values[i]);
                this.Generator.Emit(OpCodes.Stelem_Ref);
            }
            return new ILLocal(this, local);
        }
        public ILLocal NewArray(params ILData[] values)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (values == null)
                throw new ArgumentNullException("values");

            ILLocal array = this.NewArray(typeof(object), values.Length);
            for (int i = 0; i < values.Length; i++)
                array.StoreElement(i, values[i]);
            return array;
        }
        public ILLocal NewArray(Type elementType, int length)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (elementType == null)
                throw new ArgumentNullException("elementType");
            if (length < 0)
                throw new IndexOutOfRangeException("Dizi uzunluğu sıfırdan küçük olamaz.");

            LocalBuilder local = this.Generator.DeclareLocal(elementType.MakeArrayType());
            this.Generator.Emit(OpCodes.Ldc_I4, length);
            this.Generator.Emit(OpCodes.Newarr, elementType);
            this.Generator.Emit(OpCodes.Stloc, local);
            return new ILLocal(this, local);
        }
        public ILLocal NewArray(Type elementType, ILData length)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (elementType == null)
                throw new ArgumentNullException("elementType");
            if (length == null)
                throw new ArgumentNullException("length");
            if (length.ILType != typeof(int))
                throw new ArgumentException("length değerinin tipi System.Int32 türünde olmalı.");

            LocalBuilder local = this.Generator.DeclareLocal(elementType.MakeArrayType());
            ((IILPusher)length).Push();
            this.Generator.Emit(OpCodes.Newarr, elementType);
            this.Generator.Emit(OpCodes.Stloc, local);
            return new ILLocal(this, local);
        }
        public ILLocal NewObject(ConstructorInfo constructor, ILData[] parameters)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (constructor == null)
                throw new ArgumentNullException("constructor");
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            LocalBuilder local = this.Generator.DeclareLocal(constructor.ReflectedType);

            if (parameters != null)
                for (int i = 0; i < parameters.Length; i++)
                    ((IILPusher)parameters[i]).Push();

            this.Generator.Emit(OpCodes.Newobj, constructor);
            this.Generator.Emit(OpCodes.Stloc, local);
            return new ILLocal(this, local);
        }
        public ILLocal NewObject(Type objType, ILData[] parameters)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (objType == null)
                throw new ArgumentNullException("ObjectType cannot be null.");
            if (parameters == null)
                throw new ArgumentNullException("parameters");

            Type[] parameterTypes = ILExtentionUtils.ParametersToTypeList(parameters);
            ConstructorInfo ctor = ILExtentionUtils.FindConstructor(objType, parameterTypes);
            return this.NewObject(ctor, parameters);
        }
        public ILLazy Operate(ILData leftOperand, DoubleOperators doubleOperator, ILData rightOperand)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (leftOperand == null)
                throw new ArgumentNullException("leftOperand");
            if (rightOperand == null)
                throw new ArgumentNullException("rightOperand");

            Type opResultType = ILExtentionUtils.ComparePrimitiveNumberAssignation(leftOperand.ILType, rightOperand.ILType);
            if (opResultType == null)
                throw new ArgumentException("İşlemcilerden birisinin tipi System.UInt64 ise, diğer işlemci işaretsiz veya kayar nokta tipinde olmalı.");

            MethodInfo operatorOverload = ILExtentionUtils.OpOverloads(leftOperand.ILType, doubleOperator, leftOperand.ILType, rightOperand.ILType);
            if (operatorOverload == null)
            {
                operatorOverload = ILExtentionUtils.OpOverloads(rightOperand.ILType, doubleOperator, leftOperand.ILType, rightOperand.ILType);
                if (operatorOverload == null)
                {
                    if (ILExtentionUtils.IsPrimitiveNumberType(leftOperand.ILType) && ILExtentionUtils.IsPrimitiveNumberType(leftOperand.ILType))
                        return new ILLazyPrimitiveOperator(this, leftOperand, doubleOperator, rightOperand, opResultType);
                    else
                        throw new CodingException("Operandlar için herhangi bir işleç aşırı yüklemesi tanımlanmamış.");
                }
                else
                    return new ILLazyInvoke(this, null, operatorOverload, null, new ILData[] { leftOperand, rightOperand });
            }
            else
                return new ILLazyInvoke(this, null, operatorOverload, null, new ILData[] { leftOperand, rightOperand });
        }
        public ILLazy Operate(SingleOperators singleOperator, ILData operand)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (operand == null)
                throw new ArgumentNullException("operand");

            MethodInfo operatorOverload = ILExtentionUtils.OpOverloads(operand.ILType, singleOperator, operand.ILType);
            if (operatorOverload == null)
            {
                if (ILExtentionUtils.IsPrimitiveNumberType(operand.ILType))
                    return new ILLazyPrimitiveOperator(this, operand, singleOperator);
                else
                    throw new CodingException("Operand için herhangi bir işleç aşırı yüklemesi tanımlanmamış.");
            }
            else
                return new ILLazyInvoke(this, null, operatorOverload, null, new ILData[] { operand });

        }
        public void Return()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.IsVoid)
            {
                if (this.CurrentTryBlock == null)
                    this.Generator.Emit(OpCodes.Br_S, this._returnLabel);
                else
                    this.Generator.Emit(OpCodes.Leave_S, this._returnLabel);
            }
            else
                throw new CodingException("Metodun dönüş türü Void değildi. Dönüş türüne sahip metotlar bir değer döndürmelidir.");
        }
        public void Return(ILData retVal)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (this.CurrentMethod.ReturnType == typeof(void))
                throw new CodingException("Metodun dönüş türü Void türünde. Void metotlar herhangi bir değer döndüremez.");
            if (retVal == null)
                throw new ArgumentNullException("retVal");
            
            if (retVal is ILNull)
                ((IILPusher)retVal).Push();
            else if (this.CurrentMethod.ReturnType == typeof(object))
                ((IILPusher)this.Box(retVal)).Push();
            else if (retVal.ILType == this.CurrentMethod.ReturnType || (!this.CurrentMethod.ReturnType.IsGenericParameter && this.CurrentMethod.ReturnType.IsAssignableFrom(retVal.ILType)))
                ((IILPusher)retVal).Push();
            else
                ((IILPusher)retVal.Convert(this.CurrentMethod.ReturnType)).Push();

            this.Generator.Emit(OpCodes.Stloc, this._returnValue);
            if (this.CurrentTryBlock == null)
                this.Generator.Emit(OpCodes.Br_S, this._returnLabel);
            else
                this.Generator.Emit(OpCodes.Leave_S, this._returnLabel);
        }
        public void Return(string value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (value == null)
                throw new ArgumentNullException("value");
            if (this.CurrentMethod.ReturnType == value.GetType())
                this.Return(this.Constant(value));
            else
                throw new CodingException("Metodun dönüş türü System.String değildi.");
        }
        public void Throw(string message)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (message == null)
                throw new ArgumentNullException("message");
            this.Throw(typeof(Exception), new ILData[] { this.Constant(message) });
        }
        public void Throw(Type exceptionType)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (exceptionType == null)
                throw new ArgumentNullException("exceptionType");
            this.Generator.ThrowException(exceptionType);
        }
        public void Throw(Type exceptionType, ILData[] ctorParameters)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (exceptionType == null)
                throw new ArgumentNullException("exceptionType");

            ILLazy push = this.LazyConstruct(exceptionType, ctorParameters);
            ((IILPusher)push).Push();
            this.Generator.Emit(OpCodes.Throw);
        }
        public void Throw(ILData exceptionObj)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (exceptionObj == null)
                throw new ArgumentNullException("exceptionObj");

            if (typeof(Exception).IsAssignableFrom(exceptionObj.ILType))
            {
                ((IILPusher)exceptionObj).Push();
                this.Generator.Emit(OpCodes.Throw);
            }
            else
                throw new CodingException("İstisna olarak fırlatılacak objenin türü System.Exception sınıfından türemiş olmalıdır.");
        }
        public ILTry Try()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            ILTry block = new ILTry(this);
            this._openedBlocks.Add(block);
            return block;
        }
        public ILConstant TypeOf(Type value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (value == null)
                throw new ArgumentNullException("value");

            return new ILConstant(this, value);
        }
        public ILWhile While(ILLazy comparer)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            ILWhile block = new ILWhile(this, comparer);
            this._openedBlocks.Add(block);
            return block;
        }
        public void WriteLine(ILData ilValue)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            if (ilValue == null)
                throw new ArgumentNullException("ilValue");
            if (ilValue.PinnedState == PinnedState.Null)
                throw new ArgumentException("ILNull türündeki bir ILData nesnesi bu metot için kullanılamaz.");
            else if (ilValue.ILType == typeof(string))
                ((IILPusher)ilValue).Push();
            else
                ((IILPusher)ilValue).PushToString();

            this.Generator.Emit(OpCodes.Call, ILCoder.writeLine);
        }
        public void WriteLine(string value)
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");
            this.WriteLine(this.Constant(value));
        }

        void IBuilder.OnBuild()
        {
            if (this._isBuild)
                throw new CodingException("Metodun tanımlandığı tip derlenmiş. Derlenmiş metotların gövdesi yeniden yazılamaz.");

            for (int i = 0; i < this._openedBlocks.Count; i++)
            {
                if (!this._openedBlocks[i].IsEnd)
                    throw new CodingException(this._openedBlocks[i].BlockType + " tipindeki bir blok henüz sonlandırılmamış. Bloklar End() metodu ile kapatılmalıdır.");
            }
            this._openedBlocks.Clear();
            this._openedBlocks = null;

            this.Generator.MarkLabel(this._returnLabel);

            if (!this.IsVoid)
                this.Generator.Emit(OpCodes.Ldloc, this._returnValue);
            this.Generator.Emit(OpCodes.Ret);
            this._isBuild = true;
        }
        bool IBuilder.IsBuild { get { return this._isBuild; } }

        public static TReturn[] ConvertILArrays<TReturn, TParameter>(TParameter[] ilArray)
            where TReturn : ILData
            where TParameter : TReturn
        {
            TReturn[] returns = new TReturn[ilArray.Length];
            for (int i = 0; i < ilArray.Length; i++)
                returns[i] = ilArray[i];
            return returns;
        }
    }
}
