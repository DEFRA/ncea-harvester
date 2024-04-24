namespace Ncea.Harvester.Processors.Contracts;

public interface IProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken);
}