using Bulky.DataAccess.Repository.IRepository;
using Microsoft.SemanticKernel;

namespace ProjectCore.Plugins
{
    public class ChatKernelFactory : IChatKernelFactory
    {
        private readonly Kernel _baseKernel;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ProductPlugin _productPlugin;
        public ChatKernelFactory(Kernel baseKernel, IUnitOfWork unitOfWork, ProductPlugin productPlugin) {
            _baseKernel = baseKernel;
            _unitOfWork = unitOfWork;
            _productPlugin = productPlugin;
        }
        public Kernel CreateForChat(string userId) {
            var kernel = _baseKernel.Clone();

            // OrderPlugin is built per call so the user id is baked in and never
            // reaches the model's tool schema. ProductPlugin is catalogue data,
            // identical for every caller, so it stays injected.
            kernel.Plugins.AddFromObject(new OrderPlugin(_unitOfWork, userId), "OrderPlugin");
            kernel.Plugins.AddFromObject(_productPlugin, "ProductPlugin");
            return kernel;
        }
    }
}
