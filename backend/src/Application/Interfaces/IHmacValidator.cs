namespace invoice_v1.src.Application.Interfaces
{
    // Service interface for HMAC signature validation.
    public interface IHmacValidator
    {
        // Validates the HMAC signature against the request body.
        bool ValidateHmac(string requestBody, string providedHmac);

        // Computes HMAC-SHA256 signature for the given data.
        string ComputeHmac(string data);
    }
}
