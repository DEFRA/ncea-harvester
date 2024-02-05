using System;
using System.Net;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using ncea.harvester.infra;
using ncea.harvester.Models;
using ncea_harvester.tests.Clients;
using Xunit;

namespace ncea_harvester.tests.Infra
{
    public class KeyVaultServiceTests
    {
        [Fact]
        public async Task GetSecretAsync_ShouldReturnSecretValue()
        {
            // Arrange
            var keyVaultService = KeyVaultServiceForTests.Get("test-secret-key", "test-secret-value");

            // Act
            var result = await keyVaultService.GetSecretAsync("test-secret-key");

            // Assert
            Assert.Equal("test-secret-value", result);
        }        
    }
}
