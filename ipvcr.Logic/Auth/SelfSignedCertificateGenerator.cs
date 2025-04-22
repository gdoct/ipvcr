using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ipvcr.Logic.Auth
{
    public class SelfSignedCertificateGenerator
    {
        private readonly IFileSystem _fileSystem;

        public SelfSignedCertificateGenerator(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        /// <summary>
        /// Generates a self-signed TLS certificate and writes it to the specified path
        /// </summary>
        /// <param name="outputPath">Path where the certificate will be saved</param>
        /// <param name="password">Optional password to protect the certificate</param>
        /// <returns>The generated X509Certificate2</returns>
        public X509Certificate2 GenerateSelfSignedTlsCertificate(string outputPath, string? password = null)
        {
            if (string.IsNullOrEmpty(outputPath))
                throw new ArgumentException("Output path cannot be null or empty", nameof(outputPath));

            // Create a new RSA key pair
            using var rsa = RSA.Create(2048);

            // Certificate information
            var distinguishedName = new X500DistinguishedName("CN=ipvcr-self-signed-cert");

            // Certificate request with the public key
            var request = new CertificateRequest(
                distinguishedName,
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            // Add key usage extension
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: true));

            // Add enhanced key usage extension (for server authentication)
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Authentication
                    critical: true));

            // Generate a self-signed certificate
            var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(-1),  // Start date (yesterday)
                DateTimeOffset.UtcNow.AddYears(1)); // End date (1 year from now)

            // Export the certificate to a PFX file
            byte[] certificateBytes = certificate.Export(X509ContentType.Pfx, password);

            // create output folder if it does not exist
            var outputDirectory = _fileSystem.Path.GetDirectoryName(outputPath);
            if (outputDirectory != null && !_fileSystem.Directory.Exists(outputDirectory))
            {
                _fileSystem.Directory.CreateDirectory(outputDirectory);
            }
            _fileSystem.File.WriteAllBytes(outputPath, certificateBytes);

            return certificate;
        }
    }
}