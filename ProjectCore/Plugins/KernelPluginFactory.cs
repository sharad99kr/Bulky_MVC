using Bulky.DataAccess.Repository.IRepository;
using Microsoft.SemanticKernel;
using ProjectCore.Services.AI;

namespace ProjectCore.Plugins
{
    public class KernelPluginFactory : IKernelPluginFactory
    {
        private readonly Kernel _kernel;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISearchService _searchService;

        public KernelPluginFactory(Kernel kernel, IUnitOfWork unitOfWork, ISearchService searchService)
        {
            _kernel = kernel;
            _unitOfWork = unitOfWork;
            _searchService = searchService;
        }
        public Kernel CreateKernelWithPlugins() {
            var kernelWithPlugins = _kernel.Clone();
            kernelWithPlugins.Plugins.AddFromObject(new OrderPlugin(_unitOfWork));
            kernelWithPlugins.Plugins.AddFromObject(new ProductPlugin(_searchService));
            return kernelWithPlugins;
        }
    }
}
