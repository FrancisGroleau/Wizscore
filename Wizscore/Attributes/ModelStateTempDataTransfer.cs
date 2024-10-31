using Microsoft.AspNetCore.Mvc.Filters;

namespace Wizscore.Attributes
{

    public abstract class ModelStateTempDataTransfer : ActionFilterAttribute
    {
        protected static readonly string Key = typeof(ModelStateTempDataTransfer).FullName;
    }
}
