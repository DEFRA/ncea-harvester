using ncea.harvester.Enums;
using Ncea.Harvester.Enums;
using System.Net.WebSockets;

namespace Ncea.Harvester.Models;

public class HarvestedRecordMessage
{
    public HarvestedRecordMessage(string fileIdentifier, DataFormat dataFormat, DataStandard dataStandard, DataSource dataSource, MessageType messageType)
    {
        FileIdentifier = fileIdentifier;
        DataFormat = dataFormat;
        DataStandard = dataStandard;
        DataSource = dataSource;
        MessageType = messageType;
    }

    public string FileIdentifier { get; set; }
    public DataFormat DataFormat { get; set; }
    public DataStandard DataStandard { get; set; }
    public DataSource DataSource { get; set; }
    public MessageType MessageType { get; set; }
}
