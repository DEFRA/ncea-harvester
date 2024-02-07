using Azure.Security.KeyVault.Secrets;
using Ncea.Harvester.Infrastructure.Contracts;

namespace Ncea.Harvester.Infrastructure;

public class KeyVaultService: IKeyVaultService
{
    private SecretClient _secretClient;

    public KeyVaultService(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }

    public async Task<string> GetSecretAsync(string key)
    {
        var secret = await _secretClient.GetSecretAsync(key);
        return secret.Value.Value;
    }
}
