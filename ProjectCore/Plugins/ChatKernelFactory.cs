using Microsoft.SemanticKernel;

namespace ProjectCore.Plugins
{
    public class ChatKernelFactory : IChatKernelFactory
    {
        private readonly Kernel _baseKernel;
        private readonly OrderPlugin _orderPlugin;
        private readonly ProductPlugin _productPlugin;
        public ChatKernelFactory(Kernel baseKernel, OrderPlugin orderPlugin, ProductPlugin productPlugin) {
            _baseKernel = baseKernel;
            _orderPlugin = orderPlugin;
            _productPlugin = productPlugin;
        }
        public Kernel CreateForChat() {
            var kernel = _baseKernel.Clone();
            kernel.Plugins.AddFromObject(_orderPlugin, "OrderPlugin");
            kernel.Plugins.AddFromObject(_productPlugin, "ProductPlugin");
            return kernel;
        }
    }
}
