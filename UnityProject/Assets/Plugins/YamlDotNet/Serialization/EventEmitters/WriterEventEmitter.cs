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

using YamlDotNet.Core;
using YamlDotNet.Core.Events;

namespace YamlDotNet.Serialization.EventEmitters
{
    public sealed class WriterEventEmitter : IEventEmitter
    {
        void IEventEmitter.Emit(AliasEventInfo eventInfo, IEmitter emitter)
        {
            emitter.Emit(new AnchorAlias(eventInfo.Alias));
        }

        void IEventEmitter.Emit(ScalarEventInfo eventInfo, IEmitter emitter)
        {
            emitter.Emit(new Scalar(eventInfo.Anchor, eventInfo.Tag, eventInfo.RenderedValue, eventInfo.Style, eventInfo.IsPlainImplicit, eventInfo.IsQuotedImplicit));
        }

        void IEventEmitter.Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
        {
            emitter.Emit(new MappingStart(eventInfo.Anchor, eventInfo.Tag, eventInfo.IsImplicit, eventInfo.Style));
        }

        void IEventEmitter.Emit(MappingEndEventInfo eventInfo, IEmitter emitter)
        {
            emitter.Emit(new MappingEnd());
        }

        void IEventEmitter.Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
        {
            emitter.Emit(new SequenceStart(eventInfo.Anchor, eventInfo.Tag, eventInfo.IsImplicit, eventInfo.Style));
        }

        void IEventEmitter.Emit(SequenceEndEventInfo eventInfo, IEmitter emitter)
        {
            emitter.Emit(new SequenceEnd());
        }
    }
}
