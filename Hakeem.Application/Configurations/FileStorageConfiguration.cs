namespace Hakeem.Application.Configurations;

public class FileStorageConfiguration
{
    public const string SectionName = "FileStorage";

    public string BasePath { get; set; } = "wwwroot/uploads";

    public long MaxFileSizeBytes { get; set; } = 10485760;


    public string[] AllowedExtensions { get; set; } =
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".txt", ".zip", ".rar"
    };
}
