using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Petaframework.Middlewares.JwtMiddleware;

namespace Petaframework.Middlewares
{
    public class PtfkPipelineFilter : IAsyncAuthorizationFilter, IAsyncResourceFilter, IAsyncExceptionFilter, IAsyncActionFilter, IAsyncAlwaysRunResultFilter
    {
        internal readonly OIdCSettings OIdCSettings;
        public PtfkPipelineFilter(OIdCSettings oIdCSettings)
        {
            this.OIdCSettings = oIdCSettings;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            context.HttpContext.PtfkEnableSubmitter();
            await Task.CompletedTask;
        } 
        
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            await next();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await next();
        }

        public async Task OnExceptionAsync(ExceptionContext context)
        {
            await Task.CompletedTask;
        }
    }
}
