using Microsoft.SemanticKernel;

namespace ProjectCore.Filters
{
    public class AIFunctionInvocationFilter : IFunctionInvocationFilter
    {
        private readonly ILogger<AIFunctionInvocationFilter> _logger;
        public AIFunctionInvocationFilter(ILogger<AIFunctionInvocationFilter> logger)
        {
            _logger = logger;
        }

        public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next) {
            throw new NotImplementedException();
        }
    }
}
