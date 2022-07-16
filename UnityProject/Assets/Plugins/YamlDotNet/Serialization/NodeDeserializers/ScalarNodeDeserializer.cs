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
using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization.Utilities;

namespace YamlDotNet.Serialization.NodeDeserializers
{
    public sealed class ScalarNodeDeserializer : INodeDeserializer
    {
        private const string BooleanTruePattern = "^(true|y|yes|on)$";
        private const string BooleanFalsePattern = "^(false|n|no|off)$";

        bool INodeDeserializer.Deserialize(IParser parser, Type expectedType, Func<IParser, Type, object?> nestedObjectDeserializer, out object? value)
        {
            if (!parser.TryConsume<Scalar>(out var scalar))
            {
                value = null;
                return false;
            }

            // Strip off the nullable type, if present
            var underlyingType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;

            if (underlyingType.IsEnum())
            {
                value = Enum.Parse(underlyingType, scalar.Value, true);
                return true;
            }

            var typeCode = underlyingType.GetTypeCode();
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    value = DeserializeBooleanHelper(scalar.Value);
                    break;

                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    value = DeserializeIntegerHelper(typeCode, scalar.Value);
                    break;

                case TypeCode.Single:
                    value = float.Parse(scalar.Value, YamlFormatter.NumberFormat);
                    break;

                case TypeCode.Double:
                    value = double.Parse(scalar.Value, YamlFormatter.NumberFormat);
                    break;

                case TypeCode.Decimal:
                    value = decimal.Parse(scalar.Value, YamlFormatter.NumberFormat);
                    break;

                case TypeCode.String:
                    value = scalar.Value;
                    break;

                case TypeCode.Char:
                    value = scalar.Value[0];
                    break;

                case TypeCode.DateTime:
                    // TODO: This is probably incorrect. Use the correct regular expression.
                    value = DateTime.Parse(scalar.Value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    break;

                default:
                    if (expectedType == typeof(object))
                    {
                        // Default to string
                        value = scalar.Value;
                    }
                    else
                    {
                        value = TypeConverter.ChangeType(scalar.Value, expectedType);
                    }
                    break;
            }
            return true;
        }

        private object DeserializeBooleanHelper(string value)
        {
            bool result;

            if (Regex.IsMatch(value, BooleanTruePattern, RegexOptions.IgnoreCase))
            {
                result = true;
            }
            else if (Regex.IsMatch(value, BooleanFalsePattern, RegexOptions.IgnoreCase))
            {
                result = false;
            }
            else
            {
                throw new FormatException($"The value \"{value}\" is not a valid YAML Boolean");
            }

            return result;
        }

        private object DeserializeIntegerHelper(TypeCode typeCode, string value)
        {
            var numberBuilder = new StringBuilder();
            var currentIndex = 0;
            var isNegative = false;
            int numberBase;
            ulong result = 0;

            if (value[0] == '-')
            {
                currentIndex++;
                isNegative = true;
            }

            else if (value[0] == '+')
            {
                currentIndex++;
            }

            if (value[currentIndex] == '0')
            {
                // Could be binary, octal, hex, decimal (0)

                // If there are no characters remaining, it's a decimal zero
                if (currentIndex == value.Length - 1)
                {
                    numberBase = 10;
                    result = 0;
                }

                else
                {
                    // Check the next character
                    currentIndex++;

                    if (value[currentIndex] == 'b')
                    {
                        // Binary
                        numberBase = 2;

                        currentIndex++;
                    }

                    else if (value[currentIndex] == 'x')
                    {
                        // Hex
                        numberBase = 16;

                        currentIndex++;
                    }

                    else
                    {
                        // Octal
                        numberBase = 8;
                    }
                }

                // Copy remaining digits to the number buffer (skip underscores)
                while (currentIndex < value.Length)
                {
                    if (value[currentIndex] != '_')
                    {
                        numberBuilder.Append(value[currentIndex]);
                    }
                    currentIndex++;
                }

                // Parse the magnitude of the number
                switch (numberBase)
                {
                    case 2:
                    case 8:
                        // TODO: how to incorporate the numberFormat?
                        result = Convert.ToUInt64(numberBuilder.ToString(), numberBase);
                        break;

                    case 16:
                        result = ulong.Parse(numberBuilder.ToString(), NumberStyles.HexNumber, YamlFormatter.NumberFormat);
                        break;

                    case 10:
                        // Result is already zero
                        break;
                }
            }

            else
            {
                // Could be decimal or base 60
                var chunks = value.Substring(currentIndex).Split(':');
                result = 0;

                for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
                {
                    result *= 60;

                    // TODO: verify that chunks after the first are non-negative and less than 60
                    result += ulong.Parse(chunks[chunkIndex].Replace("_", ""));
                }
            }

            if (isNegative)
            {
                return CastInteger(checked(-(long)result), typeCode);
            }
            else
            {
                return CastInteger(result, typeCode);
            }
        }

        private static object CastInteger(long number, TypeCode typeCode)
        {
            checked
            {
                return typeCode switch
                {
                    TypeCode.Byte => (byte)number,
                    TypeCode.Int16 => (short)number,
                    TypeCode.Int32 => (int)number,
                    TypeCode.Int64 => number,
                    TypeCode.SByte => (sbyte)number,
                    TypeCode.UInt16 => (ushort)number,
                    TypeCode.UInt32 => (uint)number,
                    TypeCode.UInt64 => (ulong)number,
                    _ => number,
                };
            }
        }

        private static object CastInteger(ulong number, TypeCode typeCode)
        {
            checked
            {
                return typeCode switch
                {
                    TypeCode.Byte => (byte)number,
                    TypeCode.Int16 => (short)number,
                    TypeCode.Int32 => (int)number,
                    TypeCode.Int64 => (long)number,
                    TypeCode.SByte => (sbyte)number,
                    TypeCode.UInt16 => (ushort)number,
                    TypeCode.UInt32 => (uint)number,
                    TypeCode.UInt64 => number,
                    _ => number,
                };
            }
        }
    }
}
