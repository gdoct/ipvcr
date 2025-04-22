using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Security.Cryptography.X509Certificates;
using ipvcr.Logic.Auth;
using Moq;

namespace ipvcr.Tests.Auth;

public class SelfSignedCertificateGeneratorTests
{
    private readonly Mock<IFileSystem> _fileSystemMock;

    public SelfSignedCertificateGeneratorTests()
    {
        _fileSystemMock = new Mock<IFileSystem>();
    }

    [Fact]
    public void GenerateSelfSignedTlsCertificate_WithValidPath_GeneratesCertificate()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var generator = new SelfSignedCertificateGenerator(fileSystem);
        var outputPath = "/test/certificate.pfx";
        fileSystem.AddDirectory("/test");

        // Act
        var certificate = generator.GenerateSelfSignedTlsCertificate(outputPath);

        // Assert
        Assert.NotNull(certificate);
        Assert.True(certificate.HasPrivateKey);
        Assert.Contains(outputPath, fileSystem.AllFiles);
        Assert.True(fileSystem.FileExists(outputPath));
    }

    [Fact]
    public void GenerateSelfSignedTlsCertificate_WithPassword_GeneratesProtectedCertificate()
    {
        // Arrange
        var fileSystem = new MockFileSystem();
        var generator = new SelfSignedCertificateGenerator(fileSystem);
        var outputPath = "/test/protected-certificate.pfx";
        fileSystem.AddDirectory("/test");
        var password = "StrongP@ssw0rd";

        // Act
        var certificate = generator.GenerateSelfSignedTlsCertificate(outputPath, password);

        // Assert
        Assert.NotNull(certificate);
        Assert.True(certificate.HasPrivateKey);
        Assert.Contains(outputPath, fileSystem.AllFiles);

        // Verify the certificate can be loaded with the password
        byte[] pfxBytes = fileSystem.File.ReadAllBytes(outputPath);
        var loadedCert = new X509Certificate2(pfxBytes, password);
        Assert.NotNull(loadedCert);
        Assert.True(loadedCert.HasPrivateKey);
    }

    [Fact]
    public void GenerateSelfSignedTlsCertificate_WithNullOutputPath_ThrowsArgumentException()
    {
        // Arrange
        var generator = new SelfSignedCertificateGenerator(_fileSystemMock.Object);
        string? outputPath = null;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            generator.GenerateSelfSignedTlsCertificate(outputPath!));

        Assert.Equal("outputPath", exception.ParamName);
        _fileSystemMock.Verify(fs => fs.File.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public void GenerateSelfSignedTlsCertificate_WithEmptyOutputPath_ThrowsArgumentException()
    {
        // Arrange
        var generator = new SelfSignedCertificateGenerator(_fileSystemMock.Object);
        string outputPath = string.Empty;

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            generator.GenerateSelfSignedTlsCertificate(outputPath));

        Assert.Equal("outputPath", exception.ParamName);
        _fileSystemMock.Verify(fs => fs.File.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
    }

    [Fact]
    public void GenerateSelfSignedTlsCertificate_WritesToFileSystem()
    {
        // Arrange
        _fileSystemMock.Setup(fs => fs.File.WriteAllBytes(It.IsAny<string>(), It.IsAny<byte[]>()));
        _fileSystemMock.Setup(fs => fs.Directory.CreateDirectory(It.IsAny<string>())).Returns(Mock.Of<IDirectoryInfo>());
        _fileSystemMock.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(false);
        _fileSystemMock.Setup(fs => fs.Path.GetDirectoryName(It.IsAny<string>())).Returns("/test");
        var generator = new SelfSignedCertificateGenerator(_fileSystemMock.Object);
        var outputPath = "/test/certificate.pfx";

        // Act
        generator.GenerateSelfSignedTlsCertificate(outputPath);

        // Assert
        _fileSystemMock.Verify(fs => fs.File.WriteAllBytes(outputPath, It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public void Constructor_WithNullFileSystem_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SelfSignedCertificateGenerator(null!));

        Assert.Equal("fileSystem", exception.ParamName);
    }
}