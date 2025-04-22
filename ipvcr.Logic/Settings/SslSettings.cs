namespace ipvcr.Logic.Settings;

public class SslSettings
{
    // class for managing SSL settings such as certificate path, password, etc.
    public string CertificatePath { get; set; } = "/data/ssl-certificates/certificate.pfx";
    public string CertificatePassword { get; set; } = "default_password";
    public bool UseSsl { get; set; } = true;
}