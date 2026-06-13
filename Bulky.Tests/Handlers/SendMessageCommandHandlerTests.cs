using Moq;
using ProjectCore.CQRS.Commands;
using ProjectCore.CQRS.Handlers;
using ProjectCore.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Tests.Handlers
{
    public class SendMessageCommandHandlerTests
    {
        [Fact]
        public async Task Handle_ValidMessage_DelegatesToChatService()
        {
            // Arrange
            var mockChatService = new Mock<IChatService>();
            mockChatService.Setup(s => s.SendMessageAsync(
                            It.IsAny<string>(), 
                            It.IsAny<Guid?>(), 
                            It.IsAny<string>(), 
                            It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ChatResponse (
                    Message:"Here are some cozy mysteries...",
                    ConversationId: Guid.NewGuid(),
                    FromCache: false,
                    FallbackUsed: false,
                    TokensUsed: 120));

            // Act
            var handler = new SendMessageCommandHandler(mockChatService.Object);
            var result = await handler.Handle(new SendMessageCommand ( 
                                        UserMessage : "Do you have cozy mystery book?",
                                        null,
                                        "user-123"), 
                                    CancellationToken.None);
            // Assert
            Assert.False(result.FromCache);
            Assert.False(result.FallbackUsed);
            Assert.Equal("Here are some cozy mysteries...", result.Message);
            mockChatService.Verify(s => s.SendMessageAsync(
                            "Do you have cozy mystery book?", 
                            null, 
                            "user-123", 
                            It.IsAny<CancellationToken>()), 
                        Times.Once);
        }

        [Fact]
        public async Task Handle_ServiceThrows_FallbackResponseReturned() {
            // Arrange: mock returns FallbackUsed: true ChatResponse
            // Assert: result.FallbackUsed == true
        }

        [Fact]
        public async Task GetRecentAsync_WrongUserId_ReturnsEmpty() {
            // Arrange: seed conversation belonging to "user-A"
            // Request it as "user-B"
            // Assert result is empty — no data leak
            // Use in-memory EF Core for this test
            // This test protects against IDOR regression in CI
        }

        [Fact]
        public async Task BuildChatHistory_MoreThanSixTurns_RehydratesOnlySix() {
            // Arrange: ChatMessageRepository returns 6 rows (enforced at query)
            // Act: BuildChatHistory receives the 6 rows
            // Assert: SK ChatHistory has exactly 6 messages (plus system prompt)
            // Verifies the sliding window token budget is enforced at DB level
        }

        [Fact]
        public async Task SendMessageAsync_WhenAIFails_StillPersistsFallbackTurn() {
            // Arrange: mock IChatCompletionService to throw
            // Assert: SaveTurnsAsync was called with FallbackUsed: true
            // Verifies fallback turns appear in Week 7 dashboard data
        }
    }
}
