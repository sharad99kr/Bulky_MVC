using Microsoft.SemanticKernel;

namespace ProjectCore.Plugins
{
    public interface IKernelPluginFactory
    {
        Kernel CreateKernelWithPlugins();
    }
}
