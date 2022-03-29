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
using System.Globalization;
using System.Linq;

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Serialization.Converters
{
    /// <summary>
    /// This represents the YAML converter entity for <see cref="DateTime"/>.
    /// </summary>
    public class DateTimeConverter : IYamlTypeConverter
    {
        private readonly DateTimeKind kind;
        private readonly IFormatProvider provider;
        private readonly string[] formats;

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeConverter"/> class.
        /// </summary>
        /// <param name="kind"><see cref="DateTimeKind"/> value. Default value is <see cref="DateTimeKind.Utc"/>. <see cref="DateTimeKind.Unspecified"/> is considered as <see cref="DateTimeKind.Utc"/>.</param>
        /// <param name="provider"><see cref="IFormatProvider"/> instance. Default value is <see cref="CultureInfo.InvariantCulture"/>.</param>
        /// <param name="formats">List of date/time formats for parsing. Default value is "<c>G</c>".</param>
        /// <remarks>On deserializing, all formats in the list are used for conversion, while on serializing, the first format in the list is used.</remarks>
        public DateTimeConverter(DateTimeKind kind = DateTimeKind.Utc, IFormatProvider? provider = null, params string[] formats)
        {
            this.kind = kind == DateTimeKind.Unspecified ? DateTimeKind.Utc : kind;
            this.provider = provider ?? CultureInfo.InvariantCulture;
            this.formats = formats.DefaultIfEmpty("G").ToArray();
        }

        /// <summary>
        /// Gets a value indicating whether the current converter supports converting the specified type.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to check.</param>
        /// <returns>Returns <c>True</c>, if the current converter supports; otherwise returns <c>False</c>.</returns>
        public bool Accepts(Type type)
        {
            return type == typeof(DateTime);
        }

        /// <summary>
        /// Reads an object's state from a YAML parser.
        /// </summary>
        /// <param name="parser"><see cref="IParser"/> instance.</param>
        /// <param name="type"><see cref="Type"/> to convert.</param>
        /// <returns>Returns the <see cref="DateTime"/> instance converted.</returns>
        /// <remarks>On deserializing, all formats in the list are used for conversion.</remarks>
        public object ReadYaml(IParser parser, Type type)
        {
            var value = parser.Consume<Scalar>().Value;
            var style = this.kind == DateTimeKind.Local ? DateTimeStyles.AssumeLocal : DateTimeStyles.AssumeUniversal;

            var dt = DateTime.ParseExact(value, this.formats, this.provider, style);
            dt = EnsureDateTimeKind(dt, this.kind);
            return dt;
        }

        /// <summary>
        /// Writes the specified object's state to a YAML emitter.
        /// </summary>
        /// <param name="emitter"><see cref="IEmitter"/> instance.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="type"><see cref="Type"/> to convert.</param>
        /// <remarks>On serializing, the first format in the list is used.</remarks>
        public void WriteYaml(IEmitter emitter, object? value, Type type)
        {
            var dt = (DateTime)value!;
            var adjusted = this.kind == DateTimeKind.Local ? dt.ToLocalTime() : dt.ToUniversalTime();
            var formatted = adjusted.ToString(this.formats.First(), this.provider); // Always take the first format of the list.

            emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, formatted, ScalarStyle.Any, true, false));
        }

        private static DateTime EnsureDateTimeKind(DateTime dt, DateTimeKind kind)
        {
            DateTime ensured;
            if (dt.Kind == DateTimeKind.Local && kind == DateTimeKind.Utc)
            {
                ensured = dt.ToUniversalTime();
                return ensured;
            }

            if (dt.Kind == DateTimeKind.Utc && kind == DateTimeKind.Local)
            {
                ensured = dt.ToLocalTime();
                return ensured;
            }

            return dt;
        }
    }
}
