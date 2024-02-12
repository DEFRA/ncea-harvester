namespace Ncea.Harvester.Infrastructure.Contracts;

public interface IKeyVaultService
{
    Task<string> GetSecretAsync(string key);
}
