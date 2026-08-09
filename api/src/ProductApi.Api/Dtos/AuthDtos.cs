using System.ComponentModel.DataAnnotations;

namespace ProductApi.Api.Dtos;

public sealed record LoginRequestDto([Required] string Username, [Required] string Password);

public sealed record LoginResponseDto(string Token, string TokenType, long ExpiresIn, string Username, string[] Roles);

public sealed record ErrorResponseDto(string Timestamp, int Status, string Error, string Message, IReadOnlyList<FieldErrorDto>? Errors = null);

public sealed record FieldErrorDto(string Field, string Message);
