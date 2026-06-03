using Bulky.Models;
using Moq;
using ProjectCore.CQRS.Handlers;
using ProjectCore.CQRS.Queries;
using ProjectCore.Models.AI;
using ProjectCore.Services.AI;
using SearchResult = ProjectCore.Models.AI.SearchResult<Bulky.Models.Product>;

namespace Bulky.Tests;

public class SearchProductsQueryHandlerTests
{
    private readonly Mock<ISearchService> _mockSearchService;
    private readonly SearchProductsQueryHandler _handler;

    public SearchProductsQueryHandlerTests() {
        _mockSearchService = new Mock<ISearchService>();
        _handler = new SearchProductsQueryHandler( _mockSearchService.Object );
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsSearchResult() {
        //Arrange
        var fakeResult = new SearchResult {
            Items = new List<Product> {
                new Product { Id = 1, Title = "The Midnight Library" }
            },
            TopScore = 0.9f,
            LowConfidence = false

        };

        _mockSearchService
            .Setup(s => s.HybridSearchAsync(
                It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResult);

        //Act
        var result = await _handler.Handle(
            new ProjectCore.CQRS.Queries.SearchProductsQuery("midnight library"), CancellationToken.None);

        //Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.False(result.LowConfidence);
    }

    [Fact]
    public async Task Handle_LowConfidenceResult_PassesThroughFlag() {
        //Arrange
        var fakeResult = new SearchResult {
            Items = new List<Product>(),
            TopScore = 0.3f,
            LowConfidence = true

        };

        _mockSearchService
            .Setup(s => s.HybridSearchAsync(
                It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(fakeResult);

        var result = await _handler.Handle(
            new ProjectCore.CQRS.Queries.SearchProductsQuery("xyzzy"), CancellationToken.None);
            Assert.True(result.LowConfidence);
    }

    [Fact]
    public async Task Handle_QueryExpansionEnabled_PassesFlagToService() {
        _mockSearchService.Setup(s => s.HybridSearchAsync(
            "cozy mystery", 
            5, 
            true, 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SearchResult<Product> {  Items = new List<Product>() });

        await _handler.Handle(
            new SearchProductsQuery(
                "cozy mystery", 
                TopK: 5, 
                UserQueryExpansion:  true), 
            CancellationToken.None);

        _mockSearchService.Verify(
            s => s.HybridSearchAsync(
                "cozy mystery", 
                5, 
                true, 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }
}
