﻿using Ncea.Harvester.Tests.Clients;

namespace Ncea.Harvester.Tests.Infrastructure;

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
