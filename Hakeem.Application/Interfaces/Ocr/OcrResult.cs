namespace Hakeem.Application.Interfaces.Ocr;

public sealed record OcrResult(
    IReadOnlyList<OcrPageResult> Pages);

public sealed record OcrPageResult(
    int PageNumber,
    string RecognizedText,
    decimal? Confidence);
