using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Auth.Models;

[AttributeUsage(AttributeTargets.Property)]
public class MustBeTrueAttribute : ValidationAttribute, IClientModelValidator
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        value is true
            ? ValidationResult.Success
            : new ValidationResult(ErrorMessage ?? "此欄位必須勾選。");

    public void AddValidation(ClientModelValidationContext context)
    {
        context.Attributes["data-val"] = "true";
        context.Attributes["data-val-mustbetrue"] = ErrorMessage ?? "此欄位必須勾選。";
    }
}
