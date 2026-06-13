// File    : LoginCommandValidator.cs
// Module  : Auth
// Layer   : Application
// Purpose : Validate input đăng nhập trước khi vào handler (ValidationBehavior pipeline).

using FluentValidation;

namespace ICare247.Application.Features.Auth.Login;

/// <summary>Ràng buộc cơ bản cho <see cref="LoginCommand"/> — tên đăng nhập và mật khẩu bắt buộc.</summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Tên đăng nhập không được để trống.")
            .MaximumLength(100);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MaximumLength(256);

        RuleFor(x => x.TenantId)
            .GreaterThan(0).WithMessage("Thiếu thông tin tenant.");
    }
}
