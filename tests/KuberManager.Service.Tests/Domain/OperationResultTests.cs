namespace KuberManager.Service.Tests.Domain;

public class OperationResultTests
{
    [Fact]
    public void Ok_SetsSuccessTrue_WithDefaultMessage()
    {
        var result = OperationResult.Ok("corr-1");

        result.Success.Should().BeTrue();
        result.CorrelationId.Should().Be("corr-1");
        result.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Ok_WithCustomMessage_SetsMessage()
    {
        var result = OperationResult.Ok("corr-2", "Deployment scaled successfully.");

        result.Message.Should().Be("Deployment scaled successfully.");
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Fail_SetsSuccessFalse_WithMessage()
    {
        var result = OperationResult.Fail("corr-3", "k8s timeout error");

        result.Success.Should().BeFalse();
        result.CorrelationId.Should().Be("corr-3");
        result.Message.Should().Be("k8s timeout error");
    }

    [Fact]
    public void Fail_CorrelationId_IsPreserved()
    {
        var id = Guid.NewGuid().ToString();
        var result = OperationResult.Fail(id, "error");

        result.CorrelationId.Should().Be(id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("some-uuid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Ok_AcceptsAnyCorrelationId(string correlationId)
    {
        var result = OperationResult.Ok(correlationId);

        result.CorrelationId.Should().Be(correlationId);
        result.Success.Should().BeTrue();
    }
}
