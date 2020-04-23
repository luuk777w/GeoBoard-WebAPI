using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Services
{

    public class TemplateService : ITemplateService
    {
        private readonly IRazorViewEngine RazorViewEngine;
        private readonly IServiceProvider ServiceProvider;
        private readonly ITempDataProvider TempDataProvider;

        public TemplateService(
            IRazorViewEngine razorViewEngine,
            IServiceProvider serviceProvider,
            ITempDataProvider tempDataProvider)
        {
            RazorViewEngine = razorViewEngine;
            ServiceProvider = serviceProvider;
            TempDataProvider = tempDataProvider;
        }

        public string RenderTemplate<T>(string templateName, T viewModel, bool isFullPathProvided = false)
        {
            var httpContext = new DefaultHttpContext { RequestServices = ServiceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = RazorViewEngine.FindView(actionContext, templateName, false);
                if (viewResult.View == null)
                {
                    foreach (var s in viewResult.SearchedLocations)
                    {
                        Console.WriteLine(s);
                        Debugger.Log(1, "err", s);
                    }
                    throw new ArgumentNullException($"{templateName} {CultureInfo.CurrentCulture} does not match any available view");
                }

                var viewDictionary = new ViewDataDictionary<T>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = viewModel
                };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, TempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }

        public async Task<string> RenderTemplateAsync<T>(string templateName, T viewModel, bool isFullPathProvided = false)
        {
            var httpContext = new DefaultHttpContext { RequestServices = ServiceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = RazorViewEngine.FindView(actionContext, templateName, false);
                if (viewResult.View == null)
                {
                    foreach (var s in viewResult.SearchedLocations)
                    {
                        Console.WriteLine(s);
                        Debugger.Log(1, "err", s);
                    }
                    throw new ArgumentNullException($"{templateName} {CultureInfo.CurrentCulture} does not match any available view");
                }

                var viewDictionary = new ViewDataDictionary<T>(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = viewModel
                };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, TempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }

        public async Task<string> RenderTemplateAsync(string templateName, object viewModel, bool isFullPathProvided = false)
        {
            var httpContext = new DefaultHttpContext { RequestServices = ServiceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = RazorViewEngine.FindView(actionContext, templateName, false);
                if (viewResult.View == null)
                {
                    foreach (var s in viewResult.SearchedLocations)
                    {
                        Console.WriteLine(s);
                        Debugger.Log(1, "err", s);
                    }
                    throw new ArgumentNullException($"{templateName} {CultureInfo.CurrentCulture} does not match any available view");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = viewModel
                };

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, TempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }

        private string GetFileHash(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] hash = sha.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }
    }
}
