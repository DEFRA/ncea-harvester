﻿namespace Ncea.Harvester.Infrastructure.Models.Requests;

public class DeleteBlobRequest
{
    public string Blob { get; set; }
   
    public string Container { get; set; }
}
