namespace ConCancha.Web.Services;

public sealed record FileDownload(byte[] Content, string FileName, string ContentType);
