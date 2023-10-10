using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Moq;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.Commons;
using Xunit;

namespace VaultConfigurationProvider.Tests;

public class LdapVaultConfigurationProviderTests
{
    private readonly Mock<IVaultClient> _vaultClient = new();
    private readonly Secret<SecretData> _vaultSecretsConfiguration = new()
    {
        Data = new SecretData()
    };
    private readonly VaultConfigurationOptions _options = new()
    {
        BaseUrl = "BaseUrl",
        SecretsBasePath = "SecretsPath",
        SecretsSubPaths = new []{"SecretsSubPath1", "SecretsSubPath2"}
    };
    private const string InitialValue = "InitialValue";
    private const string UpdatedValue = "UpdatedlValue";


    [Fact(DisplayName = "The provider throws an exception when it is not optional and an error occurred while retrieving secrets.")]
    public void LdapVaultConfigurationProvider_ThrowsException_WhenUnableGetSecretsAndOptionalFalse()
    {
        // Arrange
        var appConfiguration = new Dictionary<string, string>
        {
            {"A", InitialValue},
        };
        _vaultSecretsConfiguration.Data.Data = new Dictionary<string, object>
        {
            {"ENV_A", UpdatedValue},
        };
        _vaultClient
            .Setup(x => x.V1.Secrets.KeyValue.V2.ReadSecretAsync(It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .Throws(() => new VaultApiException());

        // Act
        var actualConfigurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(appConfiguration)
            .Add(new VaultConfigurationSource(_vaultClient.Object, _options, optional: false));

        // Asset
        Assert.Throws<VaultApiException>(() => actualConfigurationBuilder.Build());
    }
    
    [Fact(DisplayName = "The provider does not throw an exception when it is optional and an error occurred while retrieving secrets.")]
    public void LdapVaultConfigurationProvider_DontThrowException_WhenUnableGetSecretsAndOptionalTrue()
    {
        // Arrange
        var appConfiguration = new Dictionary<string, string>
        {
            {"A", InitialValue},
        };
        _vaultSecretsConfiguration.Data.Data = new Dictionary<string, object>
        {
            {"ENV_A", UpdatedValue},
        };
        _vaultClient
            .Setup(x => x.V1.Secrets.KeyValue.V2.ReadSecretAsync(It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .Throws(() => new VaultApiException());

        // Act
        var actualConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(appConfiguration)
            .Add(new VaultConfigurationSource(_vaultClient.Object, _options, optional: true))
            .Build();

        // Asset
        Assert.Equal(InitialValue, actualConfiguration["A"]);
    }
    
    
    [Fact(DisplayName = "The provider overwrites configuration key values according to data from Vault.")]
    public void LdapVaultConfigurationProvider_OverlapConfiguration()
    {
        // Arrange
        var appConfiguration = new Dictionary<string, string>
        {
            {"A", InitialValue},
            {"A:B", InitialValue},
            {"A:B:C", InitialValue},
            {"B:C", InitialValue}
        };
        _vaultSecretsConfiguration.Data.Data = new Dictionary<string, object>
        {
            {"ENV_A", UpdatedValue},
            {"ENV_A__B", UpdatedValue},
            {"ENV_A__B__C", UpdatedValue}
        };
        _vaultClient
            .Setup(x => x.V1.Secrets.KeyValue.V2.ReadSecretAsync(It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_vaultSecretsConfiguration);

        // Act
        var actualConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(appConfiguration)
            .Add(new VaultConfigurationSource(_vaultClient.Object, _options, optional: false))
            .Build();

        // Asset
        var configurationFromVault = _vaultSecretsConfiguration.Data.Data;
        Assert.Equal((string) configurationFromVault["ENV_A"], actualConfiguration["A"]);
        Assert.Equal((string) configurationFromVault["ENV_A__B"], actualConfiguration["A:B"]);
        Assert.Equal((string) configurationFromVault["ENV_A__B__C"], actualConfiguration["A:B:C"]);
        Assert.Equal(InitialValue, actualConfiguration["B:C"]);
    }

    [Fact(DisplayName =
        "When ExceptKeys is specified on a specific key, the provider overrides the values without overwriting the specified key.")]
    public void LdapVaultConfigurationProvider_OverlapConfigurationWithoutExceptKeys_WhenExceptKeysNotNull()
    {
        // Arrange
        var appConfiguration = new Dictionary<string, string>
        {
            {"A", InitialValue},
            {"A:B", InitialValue},
            {"A:B:C", InitialValue},
        };
        _vaultSecretsConfiguration.Data.Data = new Dictionary<string, object>
        {
            {"ENV_A", UpdatedValue},
            {"ENV_A__B", UpdatedValue},
            {"ENV_A__B__C", UpdatedValue}
        };
        _vaultClient
            .Setup(x => x.V1.Secrets.KeyValue.V2.ReadSecretAsync(It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_vaultSecretsConfiguration);
        _options.ExceptKeys = new[] {"A:B:C"};

        // Act
        var actualConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(appConfiguration)
            .Add(new VaultConfigurationSource(_vaultClient.Object, _options, optional: false))
            .Build();

        // Asset
        var configurationFromVault = _vaultSecretsConfiguration.Data.Data;
        Assert.Equal((string) configurationFromVault["ENV_A"], actualConfiguration["A"]);
        Assert.Equal((string) configurationFromVault["ENV_A__B"], actualConfiguration["A:B"]);
        Assert.Equal(InitialValue, actualConfiguration["A:B:C"]);
    }

    [Fact(DisplayName =
        "When ExceptKeys is specified, blocking the entire section, the provider overrides the values without overwriting the keys containing the section name.")]
    public void LdapVaultConfigurationProvider_OverlapConfigurationWithoutExceptKeys_WhenExceptKeysWholeSection()
    {
        // Arrange
        var appConfiguration = new Dictionary<string, string>
        {
            {"A", InitialValue},
            {"A:B", InitialValue},
            {"A:B:C", InitialValue},
            {"B:A", InitialValue},
            {"B:AB", InitialValue},
            {"B:C:D", InitialValue},
        };
        _vaultSecretsConfiguration.Data.Data = new Dictionary<string, object>()
        {
            {"ENV_A", UpdatedValue},
            {"ENV_A__B", UpdatedValue},
            {"ENV_A__B__C", UpdatedValue},
            {"ENV_B__A", UpdatedValue},
            {"ENV_B__AB", UpdatedValue},
            {"ENV_B__C__D", UpdatedValue}
        };
        _vaultClient
            .Setup(x => x.V1.Secrets.KeyValue.V2.ReadSecretAsync(It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_vaultSecretsConfiguration);
        _options.ExceptKeys = new[] {"A", "B:A"};

        // Act
        var actualConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(appConfiguration)
            .Add(new VaultConfigurationSource(_vaultClient.Object, _options, optional: false))
            .Build();

        // Asset
        var configurationFromVault = _vaultSecretsConfiguration.Data.Data;
        Assert.Equal(InitialValue, actualConfiguration["A"]);
        Assert.Equal(InitialValue, actualConfiguration["A:B"]);
        Assert.Equal(InitialValue, actualConfiguration["A:B:C"]);
        Assert.Equal(InitialValue, actualConfiguration["B:A"]);
        Assert.Equal((string) configurationFromVault["ENV_B__AB"], actualConfiguration["B:AB"]);
        Assert.Equal((string) configurationFromVault["ENV_B__C__D"], actualConfiguration["B:C:D"]);
    }
    
     [Fact(DisplayName =
        "When RequiredKeys and ExceptKeys are specified, blocking the entire section, the provider overrides the values without rewriting the keys containing the section name, but with RequiredKeys.")]
     public void LdapVaultConfigurationProvider_OverlapConfigurationWithoutExceptButWithRequiredKeys_WhenExceptKeysWholeSectionAndRequiredNotNull()
    {
        // Arrange
        var appConfiguration = new Dictionary<string, string>
        {
            {"A", InitialValue},
            {"A:B", InitialValue},
            {"A:B:D", InitialValue},
            {"A:B:C", InitialValue},
            {"B:A", InitialValue},
            {"B:AB", InitialValue},
            {"B:C:D", InitialValue},
        };
        _vaultSecretsConfiguration.Data.Data = new Dictionary<string, object>()
        {
            {"ENV_A", UpdatedValue},
            {"ENV_A__B", UpdatedValue},
            {"ENV_A__B__C", UpdatedValue},
            {"ENV_A__B__D", UpdatedValue},
            {"ENV_B__A", UpdatedValue},
            {"ENV_B__AB", UpdatedValue},
            {"ENV_B__C__D", UpdatedValue}
        };
        _vaultClient
            .Setup(x => x.V1.Secrets.KeyValue.V2.ReadSecretAsync(It.IsAny<string>(), It.IsAny<int?>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(_vaultSecretsConfiguration);
        _options.ExceptKeys = new[] {"A", "B:A"};
        _options.RequiredKeys = new[] {"A:B:D", "B:A"};

        // Act
        var actualConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(appConfiguration)
            .Add(new VaultConfigurationSource(_vaultClient.Object, _options, optional: false))
            .Build();

        // Asset
        var configurationFromVault = _vaultSecretsConfiguration.Data.Data;
        Assert.Equal(InitialValue, actualConfiguration["A"]);
        Assert.Equal(InitialValue, actualConfiguration["A:B"]);
        Assert.Equal(InitialValue, actualConfiguration["A:B:C"]);
        Assert.Equal((string) configurationFromVault["ENV_A__B__D"], actualConfiguration["A:B:D"] );
        Assert.Equal((string) configurationFromVault["ENV_B__A"], actualConfiguration["B:A"]);
        Assert.Equal( (string) configurationFromVault["ENV_B__AB"], actualConfiguration["B:AB"]);
        Assert.Equal((string) configurationFromVault["ENV_B__C__D"], actualConfiguration["B:C:D"]);
    }
     
     [Fact(DisplayName = "By default provider overrides the configuration key values without DefaultExceptKeys.")]
     public void LdapVaultConfigurationProvider_OverlapConfigurationWithoutDefaultExceptKeys_WhenUseDefaultExceptKeysTrue()
     {
         // Arrange
         var appConfiguration = new Dictionary<string, string>
         {
             {"Logging", InitialValue},
             {"Logging:LogLevel:Default", InitialValue},
             {"GlobalPrefix", InitialValue},
             {"Kestrel", InitialValue},
             {"UseSwagger", InitialValue}
         };
         _vaultSecretsConfiguration.Data.Data = new Dictionary<string, object>
         {
             {"ENV_Logging", UpdatedValue},
             {"ENV_Logging__LogLevel__Default", UpdatedValue},
             {"ENV_GlobalPrefix", UpdatedValue},
             {"ENV_Kestrel", UpdatedValue},
             {"ENV_UseSwagger", UpdatedValue},
         };
         _vaultClient
             .Setup(x => x.V1.Secrets.KeyValue.V2.ReadSecretAsync(It.IsAny<string>(), It.IsAny<int?>(),
                 It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(_vaultSecretsConfiguration);
         
         // Act
         var actualConfiguration = new ConfigurationBuilder()
             .AddInMemoryCollection(appConfiguration)
             .Add(new VaultConfigurationSource(_vaultClient.Object, _options, optional: false))
             .Build();

         // Asset
         Assert.Equal(InitialValue, actualConfiguration["Logging"]);
         Assert.Equal(InitialValue, actualConfiguration["Logging:LogLevel:Default"]);
         Assert.Equal(InitialValue, actualConfiguration["Kestrel"]);
     }
     
     [Fact(DisplayName = "The provider overrides configuration key values along with DefaultExceptKeys when the flag when the flag UseDefaultExceptKeys is false.")]
     public void LdapVaultConfigurationProvider_OverlapConfigurationWithoutDefaultExceptKeys_WhenUseDefaultExceptKeysFalse()
     {
         // Arrange
         var appConfiguration = new Dictionary<string, string>
         {
             {"Logging", InitialValue},
             {"Logging:LogLevel:Default", InitialValue},
             {"GlobalPrefix", InitialValue},
             {"Kestrel", InitialValue},
             {"UseSwagger", InitialValue}
         };
         _vaultSecretsConfiguration.Data.Data = new Dictionary<string, object>
         {
             {"ENV_Logging", UpdatedValue},
             {"ENV_Logging__LogLevel__Default", UpdatedValue},
             {"ENV_GlobalPrefix", UpdatedValue},
             {"ENV_Kestrel", UpdatedValue},
             {"ENV_UseSwagger", UpdatedValue},
         };
         _vaultClient
             .Setup(x => x.V1.Secrets.KeyValue.V2.ReadSecretAsync(It.IsAny<string>(), It.IsAny<int?>(),
                 It.IsAny<string>(), It.IsAny<string>()))
             .ReturnsAsync(_vaultSecretsConfiguration);
         _options.UseDefaultExceptKeys = false;
         
         // Act
         var actualConfiguration = new ConfigurationBuilder()
             .AddInMemoryCollection(appConfiguration)
             .Add(new VaultConfigurationSource(_vaultClient.Object, _options, optional: false))
             .Build();

         // Asset
         Assert.Equal(UpdatedValue, actualConfiguration["Logging"]);
         Assert.Equal(UpdatedValue, actualConfiguration["Logging:LogLevel:Default"]);
         Assert.Equal(UpdatedValue, actualConfiguration["GlobalPrefix"]);
         Assert.Equal(UpdatedValue, actualConfiguration["Kestrel"]);
         Assert.Equal(UpdatedValue, actualConfiguration["UseSwagger"]);
     }
}