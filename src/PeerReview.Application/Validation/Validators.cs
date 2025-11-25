// File: Application/Validation/Validators.cs
// Namespace: PeerReview.Application.Validation

using FluentValidation;
using PeerReview.Application.DTOs;
using PeerReview.Domain.Enums;

namespace PeerReview.Application.Validation
{
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
            
            RuleFor(x => x.TextEn)
                .NotEmpty()
                .MaximumLength(256);

            RuleFor(x => x.Type)
                .IsInEnum();

          

            When(x => x.Type == QuestionType.SingleChoice || x.Type == QuestionType.MultiChoice, () =>
            {
                RuleFor(x => x.OptionsCsvEn)
                    .NotEmpty().WithMessage("OptionsCsv is required for choice types.")
                    .MaximumLength(512);
            });

            When(x => x.Type != QuestionType.SingleChoice && x.Type != QuestionType.MultiChoice, () =>
            {
                RuleFor(x => x.OptionsCsvEn)
                    .Empty().WithMessage("OptionsCsv should be empty for non-choice types.");
            });
        }
    }

    public class QuestionItemUpdateDtoValidator : AbstractValidator<QuestionItemUpdateDto>
    {
        public QuestionItemUpdateDtoValidator()
        {

            RuleFor(x => x.Id)
                .Must(id => !id.HasValue || id.Value > 0)
                .WithMessage("Item Id must be null (for new) or a positive integer.");

            Include(new QuestionItemUpdateDtoValidator());
        }
    }


    public class QuestionCreateDtoValidator : AbstractValidator<QuestionCreateDto>
    {
        public QuestionCreateDtoValidator()
        {
            RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(256);

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("CategoryId must be a valid positive number.");

            RuleFor(x => x.SubCategoryId)
                .Must(id => id == null || id > 0)
                .WithMessage("SubCategoryId must be null or a valid positive number.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one item is required.")
                .Must(items => items.Distinct().Count() == items.Count)
                .WithMessage("Duplicate item IDs are not allowed.");

            RuleForEach(x => x.Items)
                .Must(id => id > 0).WithMessage("Each item id must be a positive number.");
            // ملاحظة: احذف أي SetValidator(...) لأن Items = List<int>
        }
    }

    public class QuestionUpdateDtoValidator : AbstractValidator<QuestionUpdateDto>
    {
        public QuestionUpdateDtoValidator()
        {
            RuleFor(x => x.TitleEn).NotEmpty().MaximumLength(256);

            RuleFor(x => x.CategoryId)
                .GreaterThan(0).WithMessage("CategoryId must be a valid positive number.");

            RuleFor(x => x.SubCategoryId)
                .Must(id => id == null || id > 0)
                .WithMessage("SubCategoryId must be null or a valid positive number.");

            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("At least one item is required.")
                .Must(items => items.Distinct().Count() == items.Count)
                .WithMessage("Duplicate item IDs are not allowed.");

            RuleForEach(x => x.Items)
                .Must(id => id > 0).WithMessage("Each item id must be a positive number.");
        }
    }


    // ===== Answers =====
    public class AnswerCreateDtoValidator : AbstractValidator<AnswerCreateDto>
    {
        public AnswerCreateDtoValidator()
        {
            RuleFor(x => x.QuestionId)
                .GreaterThan(0).WithMessage("QuestionId must be greater than 0.");

        }
    }

    public class AnswerUpdateDtoValidator : AbstractValidator<AnswerUpdateDto>
    {
        // وسّع هنا عند الحاجة
    }
}
