using System;
using System.Reflection;
using MetalLink.Infrastructure.Persistence;
using Xunit;

namespace MetalLink.Tests.Infrastructure;

public class PostgresSequenceSynchronizerTests
{
    [Fact]
    public void EscapeSqlLiteral_ReplacesSingleQuotes_WithDoubledQuotes()
    {
        // Arrange
        var method = typeof(PostgresSequenceSynchronizer)
            .GetMethod("EscapeSqlLiteral", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        // Act
        var result = (string)method!.Invoke(null, new object[] { "metal_link.o'hara_seq" })!;

        // Assert
        Assert.Equal("metal_link.o''hara_seq", result);
    }
}
