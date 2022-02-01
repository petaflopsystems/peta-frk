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

        //IAsyncAuthorizationFilter
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            context.HttpContext.PtfkEnableSubmitter();
            await Task.CompletedTask;
        } 
        
        //IAsyncResourceFilter
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            await next();

            //await context.HttpContext.PtfkBearerAuthenticationAsync(next, _OIdCSettings);
        }

        //IAsyncActionFilter
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await next();
            //await context.HttpContext.PtfkBearerAuthenticationAsync(next., PtfkEnvironment.CurrentEnvironment.Configuration.GetSection("OpenId").ToOIdcSettings());
        }

        //IAsyncAlwaysRunResultFilter
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            await next();
        }

        //IAsyncExceptionFilter
        public async Task OnExceptionAsync(ExceptionContext context)
        {
            await Task.CompletedTask;
        }



    }
}
