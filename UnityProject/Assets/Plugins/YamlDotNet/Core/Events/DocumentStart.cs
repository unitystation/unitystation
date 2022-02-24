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

using YamlDotNet.Core.Tokens;

namespace YamlDotNet.Core.Events
{
    /// <summary>
    /// Represents a document start event.
    /// </summary>
    public sealed class DocumentStart : ParsingEvent
    {
        /// <summary>
        /// Gets a value indicating the variation of depth caused by this event.
        /// The value can be either -1, 0 or 1. For start events, it will be 1,
        /// for end events, it will be -1, and for the remaining events, it will be 0.
        /// </summary>
        public override int NestingIncrease => 1;

        /// <summary>
        /// Gets the event type, which allows for simpler type comparisons.
        /// </summary>
        internal override EventType Type => EventType.DocumentStart;

        /// <summary>
        /// Gets the tags.
        /// </summary>
        /// <value>The tags.</value>
        public TagDirectiveCollection? Tags { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        public VersionDirective? Version { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is implicit.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is implicit; otherwise, <c>false</c>.
        /// </value>
        public bool IsImplicit { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStart"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="tags">The tags.</param>
        /// <param name="isImplicit">Indicates whether the event is implicit.</param>
        /// <param name="start">The start position of the event.</param>
        /// <param name="end">The end position of the event.</param>
        public DocumentStart(VersionDirective? version, TagDirectiveCollection? tags, bool isImplicit, Mark start, Mark end)
            : base(start, end)
        {
            this.Version = version;
            this.Tags = tags;
            this.IsImplicit = isImplicit;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStart"/> class.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="tags">The tags.</param>
        /// <param name="isImplicit">Indicates whether the event is implicit.</param>
        public DocumentStart(VersionDirective? version, TagDirectiveCollection? tags, bool isImplicit)
            : this(version, tags, isImplicit, Mark.Empty, Mark.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStart"/> class.
        /// </summary>
        /// <param name="start">The start position of the event.</param>
        /// <param name="end">The end position of the event.</param>
        public DocumentStart(Mark start, Mark end)
            : this(null, null, true, start, end)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentStart"/> class.
        /// </summary>
        public DocumentStart()
            : this(null, null, true, Mark.Empty, Mark.Empty)
        {
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return $"Document start [isImplicit = {IsImplicit}]";
        }

        /// <summary>
        /// Invokes run-time type specific Visit() method of the specified visitor.
        /// </summary>
        /// <param name="visitor">visitor, may not be null.</param>
        public override void Accept(IParsingEventVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
