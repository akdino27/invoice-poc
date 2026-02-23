namespace invoice_v1.src.Domain.Enums
{
    public enum SecurityFailReason
    {
        InvalidMimeType,
        MimeExtensionMismatch,
        MagicBytesMismatch,
        TokenLimitExceeded,
        UnsupportedType,
        MalwareDetected
    }
}
