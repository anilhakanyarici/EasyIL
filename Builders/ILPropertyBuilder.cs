using System;
using System.Reflection;
using System.Reflection.Emit;

namespace NetworkIO.ILEmitter
{
    public class ILPropertyBuilder
    {
        private static MethodAttributes _propMethodAttributes = MethodAttributes.SpecialName | MethodAttributes.HideBySig;

        private FieldBuilder _defaultField;
        private ILMethodBuilder _getMethodBuilder;
        private ILMethodBuilder _setMethodBuilder;
        private ILCoder _getCoder;
        private ILCoder _setCoder;

        public PropertyInfo BaseProperty { get; private set; }
        public ILMethodBuilder GetMethod { get { if (this._getMethodBuilder == null) return null; else return this._getMethodBuilder; } }
        public AccessModifiers GetModifier { get { if (this._getMethodBuilder == null) throw new MemberNotFoundException("Get metodu tanımlanmamış."); else return this._getMethodBuilder.Modifier; } }
        public ILTypeBuilder ILTypeBuilder { get; private set; }
        public bool IsDefaultProperty { get; private set; }
        public bool IsOverride { get; private set; }
        public bool IsStatic { get; private set; }
        public string Name { get; private set; }
        public PropertyBuilder PropertyBuilder { get; private set; }
        public AccessModifiers PropertyModifier { get { if (this._setMethodBuilder == null) return this._getMethodBuilder.Modifier; else if (this._getMethodBuilder == null) return this._setMethodBuilder.Modifier; else if (this.GetModifier > this.SetModifier) return GetModifier; else return this.SetModifier; } }
        public Type PropertyType { get; private set; }
        public ILMethodBuilder SetMethod { get { if (this._setMethodBuilder == null) return null; else return this._setMethodBuilder; } }
        public AccessModifiers SetModifier { get { if (this._setMethodBuilder == null) throw new MemberNotFoundException("Set metodu tanımlanmamış."); else return this._setMethodBuilder.Modifier; } }

        internal ILPropertyBuilder(ILTypeBuilder typeBuilder, string name, Type propertyType, bool isStatic)
        {
            this.ILTypeBuilder = typeBuilder;
            this.PropertyType = propertyType;
            this.PropertyBuilder = this.ILTypeBuilder.TypeBuilder.DefineProperty(name, System.Reflection.PropertyAttributes.None, propertyType, Type.EmptyTypes);
            this.Name = name;
            this.IsStatic = isStatic;
        }
        internal ILPropertyBuilder(ILTypeBuilder typeBuilder, PropertyInfo baseProperty)
        {
            this.ILTypeBuilder = typeBuilder;
            this.PropertyType = baseProperty.PropertyType;
            this.PropertyBuilder = this.ILTypeBuilder.TypeBuilder.DefineProperty(baseProperty.Name, baseProperty.Attributes, baseProperty.PropertyType, Type.EmptyTypes);
            this.Name = baseProperty.Name;

            if (baseProperty.GetGetMethod() != null)
                this._getMethodBuilder = typeBuilder.DefineOverrideMethod(baseProperty.GetGetMethod());
            if (baseProperty.GetSetMethod() != null)
                this._setMethodBuilder = typeBuilder.DefineOverrideMethod(baseProperty.GetSetMethod());
            if (this._setMethodBuilder == null && this._getMethodBuilder == null)
                throw new BuildException("Property does not contain any method.");
            if(this._setMethodBuilder == null ? false : this._setMethodBuilder.IsStatic || this._getMethodBuilder == null ? false : this._getMethodBuilder.IsStatic)
                throw new BuildException("Static property cannot override.");

            this.BaseProperty = baseProperty;
            this.IsOverride = true;
        }

        public void CodingAsDefault()
        {
            if (this._getMethodBuilder == null || this._setMethodBuilder == null)
                throw new BuildException("Varsayılan özellik için öncelikle Get ve Set metotları tanımlanmalı.");

            if (this._getCoder != null || this._setCoder != null)
                throw new BuildException("Get veya Set metodundan herhangi birisinin gövdesi oluşturulmuş. Varsayılan olarak atanamaz.");

            string defaultFieldName = "<k>__" + this.Name;
            FieldAttributes fieldAttributes = this.IsStatic ? FieldAttributes.Static | FieldAttributes.Private : FieldAttributes.Private;
            if (this._defaultField == null)
                this._defaultField = this.ILTypeBuilder.DefineField(this.PropertyType, defaultFieldName, fieldAttributes);

            ILCoder getCoding = this.GetGetCoder();
            ILField getField = this.IsStatic ? getCoding.GetStaticField(defaultFieldName) : getCoding.This.GetField(defaultFieldName);
            getCoding.Return(getField);

            ILCoder setCoding = GetSetCoder();
            ILField setField = this.IsStatic ? setCoding.GetStaticField(defaultFieldName) : setCoding.This.GetField(defaultFieldName);
            setField.AssignFrom(setCoding.Arg0);
            setCoding.Return();

            this.IsDefaultProperty = true;
        }
        public void DefineGetMethod(AccessModifiers modifier)
        {
            if (this.IsDefaultProperty)
                throw new DefinitionException("Özellik zaten varsayılan olarak atanmış. Yeni bir ekleme yapılamaz.");
            if (this._getMethodBuilder != null)
                throw new DefinitionException("Get metodu daha önce tanımlanmış. Geçersiz kılınmış bir özellik olabilir.");

            MethodAttributes attributes = MethodAttributes.ReuseSlot | ILPropertyBuilder._propMethodAttributes;
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
            if (this.IsStatic)
                attributes |= MethodAttributes.Static;

            ILMethodBuilder getBuilder = this.ILTypeBuilder.DefineMethod("get_" + this.Name, attributes);
            getBuilder.SetReturnType(this.PropertyType);
            this.PropertyBuilder.SetGetMethod(getBuilder.MethodBuilder);
            this._getMethodBuilder = getBuilder;
        }
        public void DefineSetMethod(AccessModifiers modifier)
        {
            if (this.IsDefaultProperty)
                throw new DefinitionException("Özellik zaten varsayılan olarak atanmış. Yeni bir ekleme yapılamaz.");
            if (this._setMethodBuilder != null)
                throw new DefinitionException("Set metodu daha önce tanımlanmış. Geçersiz kılınmış bir özellik olabilir.");

            MethodAttributes attributes = MethodAttributes.ReuseSlot | ILPropertyBuilder._propMethodAttributes;
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
            if (this.IsStatic)
                attributes |= MethodAttributes.Static;

            ILMethodBuilder setBuilder = this.ILTypeBuilder.DefineMethod("set_" + this.Name, attributes);
            setBuilder.AddParameter(this.PropertyType, "value", ParameterAttributes.None);
            setBuilder.SetReturnType(typeof(void));
            this.PropertyBuilder.SetGetMethod(setBuilder.MethodBuilder);
            this._setMethodBuilder = setBuilder;
        }
        public ILCoder GetGetCoder()
        {
            if (this._getCoder == null)
            {
                if (this.IsDefaultProperty)
                    throw new BuildException("Varsayılan olarak atanmış özelliklerin gövdesi oluşturulamaz.");
                if (this._getMethodBuilder == null)
                    throw new CodingException("Get metodu tanımlanmamış.");
                else
                    return this._getCoder = this._getMethodBuilder.GetCoder();
            }
            else
                return this._getCoder;

        }
        public ILCoder GetSetCoder()
        {
            if (this._setCoder == null)
            {
                if (this.IsDefaultProperty)
                    throw new BuildException("Varsayılan olarak atanmış özelliklerin gövdesi oluşturulamaz.");
                if (this._setMethodBuilder == null)
                    throw new CodingException("Set metodu tanımlanmamış.");
                else
                    return this._setCoder = this._setMethodBuilder.GetCoder();
            }
            else
                return this._setCoder;
        }
    }
}
