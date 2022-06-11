﻿/* http://www.zkea.net/ 
 * Copyright (c) ZKEASOFT. All rights reserved. 
 * http://www.zkea.net/licenses */

using System;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.IO;
using Easy.Serializer;

namespace Easy.Net.WebApi
{
    public class JsonSerializer : ISerializer
    {
        public string GetContentTypeRegexPattern()
        {
            return MimeContentType.Json;
        }

        public object Decode(HttpContent content, Type responseType)
        {
            var jsonString = content.ReadAsStringAsync().Result;

            return JsonConverter.Deserialize(jsonString, responseType);
        }

        public HttpContent Encode(HttpRequest request)
        {
            return new StringContent(JsonConverter.Serialize(request.Body), System.Text.Encoding.UTF8, MimeContentType.Json);
        }
    }
}
