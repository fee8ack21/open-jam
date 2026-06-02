using System.ComponentModel.DataAnnotations;
using Auth.Common.Validators;

namespace Auth.Models;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ValidPasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context)
    {
        if (!PasswordValidator.IsValid(value as string, out var error))
            return new ValidationResult(error);
        return ValidationResult.Success;
    }
}
