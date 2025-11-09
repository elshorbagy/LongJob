using LongJob.Domain.Processing;

namespace LongJob.Test.UnitTests.Domain;

[TestClass]
public class TextProcessingServiceTests
{
    [TestMethod]
    public void BuildOutput_SimpleInput_ReturnsGroupedPlusBase64()
    {
        var result = TextProcessingService.BuildOutput("aab");
        Assert.AreEqual("a2b1/YWFi", result);
    }

    [TestMethod]
    public void BuildOutput_IsCaseSensitive_AndOrdersByCharCode()
    {
        var result = TextProcessingService.BuildOutput("Aa");
        Assert.AreEqual("A1a1/QWE=", result);
    }

    [TestMethod]
    public void BuildOutput_OrdersByKey_NotByOccurrence()
    {
        var result = TextProcessingService.BuildOutput("bbaaa");
        Assert.AreEqual("a3b2/YmJhYWE=", result);
    }

    [TestMethod]
    public void BuildOutput_EmptyString_ReturnsSlashOnly()
    {
        var result = TextProcessingService.BuildOutput(string.Empty);
        Assert.AreEqual("/", result);
    }

    [TestMethod]
    public void BuildOutput_Null_ThrowsArgumentNullException()
    {        
        Assert.ThrowsException<ArgumentNullException>(() =>
        {
            _ = TextProcessingService.BuildOutput(null!);
        });
    }
}
