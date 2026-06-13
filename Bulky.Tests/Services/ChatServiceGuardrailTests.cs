using ProjectCore.Services.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.Tests.Services
{
    public class ChatServiceGuardrailTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void BuildSystemPrompt_EmptyRagContext_ReturnsNoContextVariant(string ragContext) {
            // Arrange
            var prompt = ChatPromptBuilder.BuildSystemPrompt(ragContext);

            // Assert
            Assert.Contains("No product context is available", prompt);
        }

        [Fact]
        public void BuildSystemPrompt_WithRagContext_ContainsGroundingGuardrail() {
            // Arrange
            var ragContext = "PRODUCT CATALOGUE CONTEXT:\n- The Midnight Library...";
            var prompt = ChatPromptBuilder.BuildSystemPrompt(ragContext);

            // Assert
            Assert.Contains("Answer ONLY using the product data below", prompt);
        }

        [Fact]
        public void BuildSystemPrompt_AlwaysContainsPiiGuardrail() {
            // Arrange
            var prompt = ChatPromptBuilder.BuildSystemPrompt(string.Empty);
            // Assert
            Assert.Contains("Never reveal personal information", prompt);
        }
    }
}
