using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Options;
using ncea.harvester.Models;

namespace ncea.harvester.infra
{
    public interface IKeyVaultService
    {
        Task<string> GetSecretAsync(string key);
    }
    public class KeyVaultService: IKeyVaultService
    {
        private readonly AppSettings _appSettings;
        private SecretClient _secretClient;

        public KeyVaultService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
            var _keyVaultEndpoint = new Uri(_appSettings.KeyVaultUri);
            _secretClient = new SecretClient(_keyVaultEndpoint, new DefaultAzureCredential());
        }

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
}
