using Ncea.Harvester.Enums;

namespace Ncea.Harvester.Models;

public class HarvestedRecordMessage
{
    public HarvestedRecordMessage(string fileIdentifier, DataFormat dataFormat, DataStandard dataStandard, DataSource dataSource)
    {
        FileIdentifier = fileIdentifier;
        DataFormat = dataFormat;
        DataStandard = dataStandard;
        DataSource = dataSource;
    }

    public string FileIdentifier { get; set; }
    public DataFormat DataFormat { get; set; }
    public DataStandard DataStandard { get; set; }
    public DataSource DataSource { get; set; }
}
