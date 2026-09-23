using System.ComponentModel.DataAnnotations;

namespace Sv.Order.DTOs;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DecimalScaleAttribute(int places) : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        value is null || value is decimal number && decimal.Round(number, places) == number;

    public override string FormatErrorMessage(string name) =>
        $"{name} chỉ được có tối đa {places} chữ số thập phân.";
}
