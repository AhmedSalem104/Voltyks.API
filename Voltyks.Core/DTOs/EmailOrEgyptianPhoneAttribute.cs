using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Voltyks.Core.DTOs
{
    public class EmailOrEgyptianPhoneAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            string? input = value as string;
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ValidationResult("Phone number or email is required");
            }

            // تحقق من الإيميل
            var isEmail = new EmailAddressAttribute().IsValid(input);

            // تحقق من رقم الهاتف المصري
            var isEgyptianPhone = Regex.IsMatch(input, @"^(?:\+20|0020|0)?1[0125]\d{8}$");

            if (!isEmail && !isEgyptianPhone)
            {
                return new ValidationResult("Must be a valid Egyptian phone number or email address");
            }

            return ValidationResult.Success!;
        }
    }
}

