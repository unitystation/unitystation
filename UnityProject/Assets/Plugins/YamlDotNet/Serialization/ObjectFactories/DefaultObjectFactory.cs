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
using System.Collections;
using System.Collections.Generic;

namespace YamlDotNet.Serialization.ObjectFactories
{
    /// <summary>
    /// Creates objects using Activator.CreateInstance.
    /// </summary>
    public sealed class DefaultObjectFactory : IObjectFactory
    {
        private readonly Dictionary<Type, Type> DefaultGenericInterfaceImplementations = new Dictionary<Type, Type>
        {
            { typeof(IEnumerable<>), typeof(List<>) },
            { typeof(ICollection<>), typeof(List<>) },
            { typeof(IList<>), typeof(List<>) },
            { typeof(IDictionary<,>), typeof(Dictionary<,>) }
        };

        private readonly Dictionary<Type, Type> DefaultNonGenericInterfaceImplementations = new Dictionary<Type, Type>
        {
            { typeof(IEnumerable), typeof(List<object>) },
            { typeof(ICollection), typeof(List<object>) },
            { typeof(IList), typeof(List<object>) },
            { typeof(IDictionary), typeof(Dictionary<object, object>) }
        };

        public DefaultObjectFactory()
        {
        }

        public DefaultObjectFactory(IDictionary<Type, Type> mappings)
        {
            foreach (var pair in mappings)
            {
                if (!pair.Key.IsAssignableFrom(pair.Value))
                {
                    throw new InvalidOperationException($"Type '{pair.Value}' does not implement type '{pair.Key}'.");
                }

                DefaultNonGenericInterfaceImplementations.Add(pair.Key, pair.Value);
            }
        }

        public object Create(Type type)
        {
            if (type.IsInterface())
            {
                if (type.IsGenericType())
                {
                    if (DefaultGenericInterfaceImplementations.TryGetValue(type.GetGenericTypeDefinition(), out var implementationType))
                    {
                        type = implementationType.MakeGenericType(type.GetGenericArguments());
                    }
                }
                else
                {
                    if (DefaultNonGenericInterfaceImplementations.TryGetValue(type, out var implementationType))
                    {
                        type = implementationType;
                    }
                }
            }

            try
            {
                return Activator.CreateInstance(type)!;
            }
            catch (Exception err)
            {
                var message = $"Failed to create an instance of type '{type.FullName}'.";
                throw new InvalidOperationException(message, err);
            }
        }
    }
}
