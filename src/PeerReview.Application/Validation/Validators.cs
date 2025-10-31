using FluentValidation;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Enums;

namespace PeerReview.Application.Validation;
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(64);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().MinimumLength(3).MaximumLength(64);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.RoleId).GreaterThan(0);
    }
}
public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto>
{
    public UserUpdateDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
public class QuestionItemCreateDtoValidator : AbstractValidator<QuestionItemCreateDto>
{
    public QuestionItemCreateDtoValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Type).IsInEnum();
        When(x => x.Type == QuestionType.SingleChoice || x.Type == QuestionType.MultiChoice, () =>
        {
            RuleFor(x => x.OptionsCsv).NotEmpty().WithMessage("OptionsCsv is required for choice types.");
        });
    }
}
public class QuestionCreateDtoValidator : AbstractValidator<QuestionCreateDto>
{
    public QuestionCreateDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleForEach(x => x.Items).SetValidator(new QuestionItemCreateDtoValidator());
    }
}
public class QuestionUpdateDtoValidator : AbstractValidator<QuestionUpdateDto>
{
    public QuestionUpdateDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleForEach(x => x.Items).SetValidator(new QuestionItemCreateDtoValidator());
    }
}
public class AnswerCreateDtoValidator : AbstractValidator<AnswerCreateDto>
{
    public AnswerCreateDtoValidator()
    {
        RuleFor(x => x.QuestionId).GreaterThan(0);
    }
}
public class AnswerUpdateDtoValidator : AbstractValidator<AnswerUpdateDto> { }
