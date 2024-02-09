//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.SCIM.WebHostSample
{
    public class Program
        {
            public static void Main(string[] args)
            {
                CreateHostBuilder(args).Build().Run();
            }

            public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.Configure(app =>
                        {
                            app.UseRouting();

                            app.UseEndpoints(endpoints =>
                            {
                                endpoints.MapGet("/", async context =>
                                {
                                    await context.Response.WriteAsync("Hello World!");
                                });
                            });

                            app.UseHttpRequestMessageConverter();
                        });
                    });
        }

        public class HttpRequestMessageMiddleware
        {
            private readonly RequestDelegate _next;

            public HttpRequestMessageMiddleware(RequestDelegate next)
            {
                _next = next;
            }

            public async Task Invoke(HttpContext context)
            {
                // Create the HttpRequestMessage
                var httpRequestMessage = new HttpRequestMessage()
                {
                    Method = new HttpMethod(context.Request.Method),
                    RequestUri = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}"),
                    Content = await GetRequestContent(context.Request).ConfigureAwait(false)
                };

                // Copy headers from HttpContext.Request to HttpRequestMessage
                foreach (var header in context.Request.Headers)
                {
                    httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }

                context.Items["HttpRequestMessage"] = httpRequestMessage;

                await _next(context);
            }

            private async Task<HttpContent> GetRequestContent(HttpRequest request)
            {
                var content = new MemoryStream();
                await request.Body.CopyToAsync(content).ConfigureAwait(false);
                content.Seek(0, SeekOrigin.Begin);
                return new StreamContent(content);
            }
        }

        public static class HttpRequestMessageMiddlewareExtensions
        {
            public static IApplicationBuilder UseHttpRequestMessageConverter(this IApplicationBuilder builder)
            {
                return builder.UseMiddleware<HttpRequestMessageMiddleware>();
            }
        }
    }