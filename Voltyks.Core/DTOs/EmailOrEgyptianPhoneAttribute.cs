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


            var isEgyptianPhone = Regex.IsMatch(input, @"^(\+2(010\d{8}|011\d{8}|012\d{8}|015\d{8})|010\d{8}|011\d{8}|012\d{8}|015\d{8})$");

            if (!isEmail && !isEgyptianPhone)
            {
                return new ValidationResult("Must be a valid Egyptian phone number or email address");
            }

            return ValidationResult.Success!;
        }
    }
}

