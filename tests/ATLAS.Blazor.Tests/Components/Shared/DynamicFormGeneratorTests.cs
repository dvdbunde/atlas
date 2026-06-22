using ATLAS.Blazor.Components.Shared;
using ATLAS.Blazor.FormModel;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace ATLAS.Blazor.Tests.Components.Shared;

public class DynamicFormGeneratorTests : BunitContext
{
    private readonly IReadOnlyList<DynamicFormFieldViewModel> _allFieldTypes;

    public DynamicFormGeneratorTests()
    {
        _allFieldTypes = new[]
        {
            new DynamicFormFieldViewModel
            {
                FieldName = "PropertyAddress",
                Label = "Property Address",
                Type = FieldType.Text,
                IsRequired = true,
                DefaultValue = string.Empty,
                CurrentValue = string.Empty,
                SortOrder = 1
            },
            new DynamicFormFieldViewModel
            {
                FieldName = "Description",
                Label = "Description",
                Type = FieldType.MultilineText,
                IsRequired = false,
                DefaultValue = string.Empty,
                CurrentValue = string.Empty,
                SortOrder = 2
            },
            new DynamicFormFieldViewModel
            {
                FieldName = "SquareFootage",
                Label = "Square Footage",
                Type = FieldType.Number,
                IsRequired = true,
                DefaultValue = "0",
                CurrentValue = "0",
                SortOrder = 3
            },
            new DynamicFormFieldViewModel
            {
                FieldName = "StartDate",
                Label = "Start Date",
                Type = FieldType.Date,
                IsRequired = false,
                DefaultValue = string.Empty,
                CurrentValue = string.Empty,
                SortOrder = 4
            },
            new DynamicFormFieldViewModel
            {
                FieldName = "IsUrgent",
                Label = "Is Urgent",
                Type = FieldType.Boolean,
                IsRequired = false,
                DefaultValue = "false",
                CurrentValue = "false",
                SortOrder = 5
            },
            new DynamicFormFieldViewModel
            {
                FieldName = "PermitType",
                Label = "Permit Type",
                Type = FieldType.Dropdown,
                IsRequired = true,
                DefaultValue = string.Empty,
                CurrentValue = string.Empty,
                SortOrder = 6
            }
        };
    }

    [Fact]
    public void Should_RenderTextInput_ForTextField()
    {
        var fields = new[] { _allFieldTypes[0] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var input = cut.Find("input[type='text']");
        Assert.NotNull(input);
        Assert.Equal("field-PropertyAddress", input.Id);
    }

    [Fact]
    public void Should_RenderLabel_ForTextField()
    {
        var fields = new[] { _allFieldTypes[0] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var label = cut.Find("label[for='field-PropertyAddress']");
        Assert.NotNull(label);
        Assert.Contains("Property Address", label.TextContent);
    }

    [Fact]
    public void Should_RenderTextArea_ForMultilineTextField()
    {
        var fields = new[] { _allFieldTypes[1] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var textarea = cut.Find("textarea");
        Assert.NotNull(textarea);
        Assert.Equal("field-Description", textarea.Id);
    }

    [Fact]
    public void Should_RenderNumberInput_ForNumberField()
    {
        var fields = new[] { _allFieldTypes[2] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var input = cut.Find("input[type='number']");
        Assert.NotNull(input);
        Assert.Equal("field-SquareFootage", input.Id);
    }

    [Fact]
    public void Should_RenderDateInput_ForDateField()
    {
        var fields = new[] { _allFieldTypes[3] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var input = cut.Find("input[type='date']");
        Assert.NotNull(input);
        Assert.Equal("field-StartDate", input.Id);
    }

    [Fact]
    public void Should_RenderCheckbox_ForBooleanField()
    {
        var fields = new[] { _allFieldTypes[4] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var checkbox = cut.Find("input[type='checkbox']");
        Assert.NotNull(checkbox);
        Assert.Equal("field-IsUrgent", checkbox.Id);
    }

    [Fact]
    public void Should_RenderSelect_ForDropdownField()
    {
        var fields = new[] { _allFieldTypes[5] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var select = cut.Find("select");
        Assert.NotNull(select);
        Assert.Equal("field-PermitType", select.Id);
    }

    [Fact]
    public void Should_RenderPlaceholderOption_ForDropdownField()
    {
        var fields = new[] { _allFieldTypes[5] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var placeholder = cut.Find("select option[value='']");
        Assert.NotNull(placeholder);
        Assert.Contains("Select permit type", placeholder.TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Should_ShowRequiredIndicator_ForRequiredFields()
    {
        var fields = new[] { _allFieldTypes[0] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var requiredIndicator = cut.Find("label span.text-danger[aria-label='Required']");
        Assert.NotNull(requiredIndicator);
        Assert.Equal("*", requiredIndicator.TextContent);
    }

    [Fact]
    public void Should_NotShowRequiredIndicator_ForOptionalFields()
    {
        var fields = new[] { _allFieldTypes[1] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var label = cut.Find("label[for='field-Description']");
        Assert.DoesNotContain("*", label.TextContent);
    }

    [Fact]
    public void Should_RenderPlainText_WhenReadOnlyMode()
    {
        var fields = new[] { _allFieldTypes[0] };

        var cut = Render<DynamicFormGenerator>(parameters =>
        {
            parameters.Add(p => p.Fields, fields);
            parameters.Add(p => p.Mode, FormFieldMode.ReadOnly);
        });

        var plaintext = cut.Find("p.form-control-plaintext");
        Assert.NotNull(plaintext);

        var inputs = cut.FindAll("input");
        Assert.Empty(inputs);
    }

    [Fact]
    public void Should_DisplayCurrentValue_WhenReadOnlyMode()
    {
        var fields = new[]
        {
            new DynamicFormFieldViewModel
            {
                FieldName = "PropertyAddress",
                Label = "Property Address",
                Type = FieldType.Text,
                IsRequired = true,
                CurrentValue = "123 Main St",
                SortOrder = 1
            }
        };

        var cut = Render<DynamicFormGenerator>(parameters =>
        {
            parameters.Add(p => p.Fields, fields);
            parameters.Add(p => p.Mode, FormFieldMode.ReadOnly);
        });

        var plaintext = cut.Find("p.form-control-plaintext");
        Assert.Contains("123 Main St", plaintext.TextContent);
    }

    [Fact]
    public void Should_ShowEmDash_WhenValueIsEmpty_AndReadOnlyMode()
    {
        var fields = new[] { _allFieldTypes[0] };

        var cut = Render<DynamicFormGenerator>(parameters =>
        {
            parameters.Add(p => p.Fields, fields);
            parameters.Add(p => p.Mode, FormFieldMode.ReadOnly);
        });

        var plaintext = cut.Find("p.form-control-plaintext");
        Assert.Contains("\u2014", plaintext.TextContent);
    }

    [Fact]
    public void Should_RenderEditableInputs_WhenEditMode()
    {
        var fields = new[] { _allFieldTypes[0] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var input = cut.Find("input[type='text']");
        Assert.NotNull(input);
        Assert.Null(input.GetAttribute("readonly"));
    }

    [Fact]
    public void Should_ShowValidationError_WhenRequiredFieldIsEmpty()
    {
        var fields = new[]
        {
            new DynamicFormFieldViewModel
            {
                FieldName = "PropertyAddress",
                Label = "Property Address",
                Type = FieldType.Text,
                IsRequired = true,
                CurrentValue = string.Empty,
                SortOrder = 1
            }
        };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var form = cut.Find("form");
        form.Submit();

        var validationMessages = cut.FindAll(".invalid-feedback span");
        Assert.NotEmpty(validationMessages);
        Assert.Contains(validationMessages, msg =>
            msg.TextContent.Contains("Property Address is required"));
    }

    [Fact]
    public void Should_NotShowValidationError_WhenRequiredFieldHasValue()
    {
        var fields = new[]
        {
            new DynamicFormFieldViewModel
            {
                FieldName = "PropertyAddress",
                Label = "Property Address",
                Type = FieldType.Text,
                IsRequired = true,
                CurrentValue = "123 Main St",
                SortOrder = 1
            }
        };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var form = cut.Find("form");
        form.Submit();

        var validationMessages = cut.FindAll(".invalid-feedback");
        Assert.Empty(validationMessages);
    }

    [Fact]
    public void Should_NotShowValidationError_WhenOptionalFieldIsEmpty()
    {
        var fields = new[]
        {
            new DynamicFormFieldViewModel
            {
                FieldName = "Description",
                Label = "Description",
                Type = FieldType.MultilineText,
                IsRequired = false,
                CurrentValue = string.Empty,
                SortOrder = 1
            }
        };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var form = cut.Find("form");
        form.Submit();

        var validationMessages = cut.FindAll(".invalid-feedback");
        Assert.Empty(validationMessages);
    }

    [Fact]
    public void Should_RenderAllFieldTypes_WhenMultipleFieldsProvided()
    {
        var fields = _allFieldTypes;

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        Assert.Single(cut.FindAll("input[type='text']"));
        Assert.Single(cut.FindAll("textarea"));
        Assert.Single(cut.FindAll("input[type='number']"));
        Assert.Single(cut.FindAll("input[type='date']"));
        Assert.Single(cut.FindAll("input[type='checkbox']"));
        Assert.Single(cut.FindAll("select"));
    }

    [Fact]
    public void Should_RenderFieldsInSortOrder()
    {
        var fields = new[]
        {
            new DynamicFormFieldViewModel
            {
                FieldName = "ZField", Label = "Z Field", Type = FieldType.Text,
                IsRequired = false, SortOrder = 2
            },
            new DynamicFormFieldViewModel
            {
                FieldName = "AField", Label = "A Field", Type = FieldType.Text,
                IsRequired = false, SortOrder = 1
            }
        };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var inputs = cut.FindAll("input[type='text']");
        Assert.Equal(2, inputs.Count);
        Assert.Equal("field-AField", inputs[0].Id);
        Assert.Equal("field-ZField", inputs[1].Id);
    }

    [Fact]
    public void Should_AssociateLabelsCorrectly_UsingForAttribute()
    {
        var fields = new[] { _allFieldTypes[0] };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        var label = cut.Find("label");
        var input = cut.Find("input");

        Assert.Equal(input.Id, label.GetAttribute("for"));
    }

    [Fact]
    public void Should_UseAriaDescribedBy_WhenFieldHasErrors()
    {
        var fields = new[]
        {
            new DynamicFormFieldViewModel
            {
                FieldName = "PropertyAddress",
                Label = "Property Address",
                Type = FieldType.Text,
                IsRequired = true,
                CurrentValue = string.Empty,
                SortOrder = 1
            }
        };

        var cut = Render<DynamicFormGenerator>(parameters =>
            parameters.Add(p => p.Fields, fields));

        cut.Find("form").Submit();

        var input = cut.Find("input[type='text']");
        var describedBy = input.GetAttribute("aria-describedby");
        Assert.NotNull(describedBy);
        Assert.Equal("field-PropertyAddress-validation", describedBy);
    }
}
