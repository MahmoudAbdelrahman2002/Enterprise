using Enterprise.Application.Common.Interfaces;

namespace Enterprise.Api.Extensions;

public static class FormFileExtensions
{
    public static ImageUploadFile ToImageUploadFile(this IFormFile file) =>
        new(file.OpenReadStream(), file.ContentType, file.FileName, file.Length);
}
