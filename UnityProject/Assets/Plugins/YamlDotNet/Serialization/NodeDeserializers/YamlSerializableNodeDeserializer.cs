﻿// This file is part of YamlDotNet - A .NET library for YAML.
// Copyright (c) Antoine Aubry and contributors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using YamlDotNet.Core;

namespace YamlDotNet.Serialization.NodeDeserializers
{
    public sealed class YamlSerializableNodeDeserializer : INodeDeserializer
    {
        private readonly IObjectFactory objectFactory;

        public YamlSerializableNodeDeserializer(IObjectFactory objectFactory)
        {
            this.objectFactory = objectFactory;
        }

        public bool Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
        {
#pragma warning disable 0618 // IYamlSerializable is obsolete
            if (typeof(IYamlSerializable).IsAssignableFrom(expectedType))
            {
                var serializable = (IYamlSerializable)objectFactory.Create(expectedType);
                serializable.ReadYaml(parser);
                value = serializable;
                return true;
            }
#pragma warning restore

            value = null;
            return false;
        }
    }
}
