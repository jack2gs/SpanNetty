﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Http.WebSockets.Extensions.Compression
{
    using System.Collections.Generic;
    using DotNetty.Transport.Channels;

    class PerMessageDeflateDecoder : DeflateDecoder
    {
        bool compressing;

        public PerMessageDeflateDecoder(bool noContext)
            : base(noContext)
        {
        }

        public override bool AcceptInboundMessage(object msg)
        {
            switch (msg)
            {
                case TextWebSocketFrame textFrame when (textFrame.Rsv & WebSocketRsv.Rsv1) > 0:
                    return true;
                case BinaryWebSocketFrame binFrame when (binFrame.Rsv & WebSocketRsv.Rsv1) > 0:
                    return true;
                case ContinuationWebSocketFrame conFrame when this.compressing:
                    return true;
                default:
                    return false;
            }
            //return ((msg is TextWebSocketFrame || msg is BinaryWebSocketFrame) && (((WebSocketFrame)msg).Rsv & WebSocketRsv.Rsv1) > 0)
            //    || (msg is ContinuationWebSocketFrame && this.compressing);
        }

        protected override int NewRsv(WebSocketFrame msg) =>
            (msg.Rsv & WebSocketRsv.Rsv1) > 0 ? msg.Rsv ^ WebSocketRsv.Rsv1 : msg.Rsv;

        protected override bool AppendFrameTail(WebSocketFrame msg) => msg.IsFinalFragment;

        protected override void Decode(IChannelHandlerContext ctx, WebSocketFrame msg, List<object> output)
        {
            base.Decode(ctx, msg, output);

            if (msg.IsFinalFragment)
            {
                this.compressing = false;
            }
            else //if (msg is TextWebSocketFrame || msg is BinaryWebSocketFrame)
            {
                switch (msg)
                {
                    case TextWebSocketFrame _:
                    case BinaryWebSocketFrame _:
                        this.compressing = true;
                        break;
                }
            }
        }
    }
}