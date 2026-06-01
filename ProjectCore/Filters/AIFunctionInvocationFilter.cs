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

        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next) {
            //Pre-invocation logic: logging, validation, etc.
            var functionName = context.Function?.Name ?? "UnknownFunction";
            var pluginName = context.Function?.PluginName ?? "UnknownPlugin";
            _logger.LogInformation("[SK Filter] Invoking function {PluginName}.{FunctionName} with parameters: {Parameters}",
                pluginName, functionName, string.Join(", ", context.Arguments.Select(a=> $"{a.Key} = {a.Value}")) );

            //Input Validation : reject susupicious inputs (example: prompt injection patterns) before they reach the function/plugin logic
            foreach(var arg in context.Arguments)
            {
                var value = arg.Value?.ToString() ?? "";

                //Block prompt injection attempts 
                if(value.Contains("ignore previous instructions", StringComparison.OrdinalIgnoreCase) || 
                                    value.Contains("system prompt", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("[SK Filter] Bloced suspecious input attempt on {Plugin}.{Function} with value: {Value}",
                    pluginName, functionName, value);
                    context.Result = new FunctionResult(context.Function, "Invalid Input Detected");
                    return; 
                }

                //Block obvious SQL injection patterns (if the function is expected to receive text inputs that could be used in DB queries)
                var sqlPatterns = new[] { "SELECT ", "INSERT ", "UPDATE ", "DELETE ", "--", ";" };
                if(sqlPatterns.Any(p => 
                                    value.Contains(p, StringComparison.OrdinalIgnoreCase))) {

                    _logger.LogWarning("[SK Filter] Blocked potential SQL injection attempt on {Plugin}.{Function} with value: {Value}",
                         pluginName, functionName, value);
                    context.Result = new FunctionResult(context.Function, "Invalid Input Detected");
                    return;
                }
            }


            await next(context); //proceed to the actual function invocation

            //Post-invocation logic: logging results, error handling, etc.
            _logger.LogInformation("[SK Filter] Completed invocation of {PluginName}.{FunctionName} with result: {Result}",
                pluginName, functionName, context.Result?.ToString() ?? "null");
        }
    }
}
