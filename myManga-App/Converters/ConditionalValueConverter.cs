using System;
using System.ComponentModel;
using System.Windows.Data;

namespace myManga_App.Converters
{
    [ValueConversion(typeof(object), typeof(object))]
    public class ConditionalValueConverter : IValueConverter
    {
        public String Reference { get; set; }

        private Object trueValue;
        private Object setTrueValue;
        public Object TrueValue
        {
            get { return trueValue; }
            set { trueValue = GenerateValue(setTrueValue = value); }
        }

        private Object falseValue;
        private Object setFalseValue;
        public Object FalseValue
        {
            get { return falseValue; }
            set { falseValue = GenerateValue(setFalseValue = value); }
        }

        private Type valueType;
        public String ValueType
        {
            get { return valueType.Name; }
            set
            {
                valueType = GetValueType(value);
                trueValue = GenerateValue(setTrueValue);
                falseValue = GenerateValue(setFalseValue);
            }
        }

        #region ConditionalValueConverter Members
        private Type GetValueType(String TypeName)
        {
            switch (TypeName)
            {
                case "float":
                case "System.Single":
                    return typeof(Single);

                case "double":
                case "System.Double":
                    return typeof(Double);

                case "short":
                case "System.Int16":
                    return typeof(Int16);

                case "int":
                case "System.Int32":
                    return typeof(Int32);

                case "long":
                case "System.Int64":
                    return typeof(Int64);

                case "string":
                case "System.String":
                    return typeof(String);

                case "char":
                case "System.Char":
                    return typeof(Char);

                case "bool":
                case "System.Boolean":
                    return typeof(Boolean);

                default:
                    return Type.GetType(TypeName);
            }
        }

        private Object GenerateValue(Object value)
        {
            if (Equals(valueType, null)) return null;
            if (Equals(value, null)) return null;

            if (Equals(value.GetType(), valueType)) return value;

            if (!Equals(value.GetType(), typeof(String)))
                throw new InvalidOperationException(String.Format(
                    "Set type must be ValueType ({0}) or string for ConditionalValueConverter.TrueValue. Got type {1}.",
                    valueType.Name,
                    value.GetType().Name));

            return TypeDescriptor.GetConverter(valueType).ConvertFromInvariantString((string)value);
        }
        #endregion

        #region IValueConverter Members
        public object Convert(Object value, Type targetType, Object parameter, System.Globalization.CultureInfo culture)
        {
            if (!Equals(targetType, valueType)) throw new NotSupportedException();

            if (Equals(value, null) || Equals(Reference, null)) return Equals(value, Reference) ? TrueValue : FalseValue;
            Object reference = Reference;
            if (value.GetType() != Reference.GetType()) reference = TypeDescriptor.GetConverter(value).ConvertFrom(reference);

            if (value.Equals(reference)) return TrueValue;
            return FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        { throw new NotImplementedException(); }

        #endregion
    }
}
