/*
The MIT License (MIT)
Copyright (c) 2021 Henning Thoele
*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnrealReplayServer
{
    public class BinaryOutputFormatter : OutputFormatter
    {
        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var contentType = context.HttpContext.Response.ContentType;
            if (contentType == "application/octet-stream")
            {
                return true;
            }

            return false;
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            byte[] data = context.Object as byte[];

            if (data != null)
            {
                await context.HttpContext.Response.BodyWriter.WriteAsync(data);
            }
        }
    }
}
