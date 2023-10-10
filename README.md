# VaultConfigurationProvider
A library with a configuration provider from HashiCorp Vault.

### Description of the problem being solved
In the conditions of collective development of the project, it may be necessary to store API keys, authorization tokens,
connection strings to various infrastructure, etc. This information is confidential and, according to the rules of secure development, cannot be placed
in the standard ```appsettings.json```.

In turn, when storing secrets locally on the developer's machine (```appsettings.Development.json```,
notepad, Word, etc.) team faces with another problem: content of the secrets always should be in an up-to-date state and agreed between all developers.
In addition to everything, there is question of finding a communication channel that ensures the safe transmission of these secrets.
As a rule, messengers, e-mail, carrier pigeons and other means of communication
do not provide adequate protection and will cause discomfort in a team of more than two people.

### Proposed solution
Keep secrets in specially designated places and use the right tools.

For example, `HashiCorp Vault` - supports different types of authentication and allows you to keep secrets safely.
This configuration provider uses authentication via the `LDAP` protocol using a login and password.

```Note:```
It is recommended to use it only for local development, when secrets are not stored in the standard ```appsettings.json```.

---
## Create User Secrets file in the terminal

Login and password are suggested to be kept in user secrets (Microsoft User Secret Manager Tool) due to the fact that this file 
is stored only on the user's device and can be changed using ```dotnet tools```.

**By default, the folder named _localdevvault_**, however, everyone can define this name for themselves.

1. Initialization of the secret directory (not created until a secret is added). Must be run in the project directory!
```csharp
   // The point is that every developer should have such a directory.
   dotnet user-secrets init --id localdev
```
2. Setting the login and password secret. Creates a directory along the path ``C:\Users \{user}\AppData\Roaming\Microsoft\UserSecrets\{YOUR_FOLDER_NAME}`` (Linux: ``~/.microsoft/usersecrets/{YOUR_FOLDER_NAME}``) with the file ``secret.json```.
    If you change your username and password, these lines should be executed again.
```csharp
    dotnet user-secrets set VaultProvider:Username "{YOUR_LOGIN}" --id localdevvault
    dotnet user-secrets set VaultProvider:Password "{YOUR_PASSWORD}" --id localdevvault
```
3. Checking that secrets are established.
 ```csharp
    dotnet user-secrets list --id localdevvault
```

---
## Install library
1. Create a secret file through the terminal (there is an instruction above) or using the IDE.
2. Install ```VaultConfigurationProvider```.
3. In the ```Program.cs``` class register a new configuration provider from the Vault.

The following setup is sufficient for local development:
```csharp
 builder.Configuration
        .AddJsonFile("appsettings.json")
        ...
        .AddLocalDevelopmentVault(options => {
             options.SecretsSubPaths = new[] {"somepath/service_name1"};
        }, optional: false);
```
---
## Available Settings
1. ``BaseUrl`` - address of the Vault instance, by default.
2. ``MountPoint`` -  type of binding to secrets, by default ``secret``.
3. ``SecretsBasePath`` - base path to the Vault sector.
4. ``SecretsCommonPath`` - base path to shared secrets.
5. ``UseSecretsCommonPath`` - indicates use SecretsCommonPath, by default ``true``
6. ``SecretsSubPaths`` - path to the service secrets (there may be several). Example: ``sector/service_name``.
7. ``DefaultExceptKeys`` set of default keys that are not overridden in the configuration even if they are entered in the Vault.
8. ``UseDefaultExceptKeys`` - whether to skip keys from the list ``DefaultExceptKeys``, by default ``true``.
9. ``ExceptKeys`` - keys that do not need to be redefined. You can specify the entire section. Example: ``["App", "Db:ConnectionString"]`` .
10. ``RequiredKeys`` - keys that need to be redefined even if they fall under the rules of ``ExceptKeys``. Only the full hierarchy of the key, the section is not allowed.
11. ``EnvironmentVariablePrefix`` is a prefix used to filter environment variables. By default, ``ENV_``.
12. ``SourceSeparator`` - separator for the hierarchical key in the repository, by default ``__``.
13. ``DestinationSeparator`` - separator for the hierarchical key in the application configuration, by default ``:``.

Advanced Setup:
```csharp
        builder.AddLocalDevelopmentVault(options => {
             options.SecretsBasePath = "somepath/anothepath";
             options.SecretsCommonPath = "_shared/new_common"
             options.SecretsSubPaths = new[] {"sector/service_name1"};
             options.ExceptKeys = new[] {"App:DbConnectionString", "SomeSection"};
             options.RequiredKeys = "SomeSection:Settings:BaseUrl";
        }, optional: false);
```