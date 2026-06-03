using Moq;
using ProjectCore.CQRS.Commands;
using ProjectCore.CQRS.Handlers;
using ProjectCore.Models.AI;
using ProjectCore.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Tests
{
    public class GenerateDescriptionCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidCommand_DelegatesToProductAIService() {
            var request = new AIProductDescriptionRequest {
                ProductName = "The Midnight Library",
                Category = "Fiction",
                Author = "Matt Haig",
                Tone = DescriptionTone.Professional,
                MaxSentences = 3
            };

            var expectedResponse = AIResponse<AIProductDescriptionResult>.Ok(
                
                new AIProductDescriptionResult { 
                    Description = "A gripping tale of ...",
                    Tone = "Professional"
                }
                
            );

            var mockService = new Mock<IProductAIService>();
            mockService.Setup(s => s.GenerateDescriptionAsync(
                It.IsAny<AIProductDescriptionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

            var handler = new GenerateDescriptionCommandHandler(mockService.Object);
            var result = await handler.Handle(
                new GenerateDescriptionCommand(request), CancellationToken.None);

            Assert.True(result.Success);
            Assert.Equal("A gripping tale of ...", result.Data?.Description);

            mockService.Verify(
                s => s.GenerateDescriptionAsync(
                    It.IsAny<AIProductDescriptionRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Once
                );
        }
    }
}
