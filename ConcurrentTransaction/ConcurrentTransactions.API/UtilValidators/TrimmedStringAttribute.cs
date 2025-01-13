using System.ComponentModel.DataAnnotations;

namespace ConcurrentTransactions.API.Util
{/// <summary>
/// A validation attributes that enforces a trim on string properties
/// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class TrimmedAttribute : ValidationAttribute
    {
        private readonly bool _enforceTrim;

        public TrimmedAttribute(bool enforceTrim = false)
        {
            _enforceTrim = enforceTrim;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string strValue)
            {
                var trimmedValue = strValue.Trim();

                if (!string.Equals(strValue, trimmedValue, StringComparison.Ordinal))
                {
                    if (_enforceTrim)
                    {   // if set to true alter the property to make it trimmed 
                        var property = validationContext.ObjectType.GetProperty(validationContext.MemberName!);
                        property?.SetValue(validationContext.ObjectInstance, trimmedValue);
                        return ValidationResult.Success;
                    }
                }
            }

            return ValidationResult.Success;
        }
    }
}
