namespace Ncea.Harvester.BusinessExceptions;

public class DataSourceConnectionException : BusinessException
{
    public DataSourceConnectionException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
