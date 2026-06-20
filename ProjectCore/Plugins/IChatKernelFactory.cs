using Microsoft.SemanticKernel;

namespace ProjectCore.Plugins
{
    public interface IChatKernelFactory
    {
        Kernel CreateForChat();
    }
}
