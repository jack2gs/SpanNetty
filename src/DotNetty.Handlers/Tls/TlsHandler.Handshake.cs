﻿/*
 * Copyright 2012 The Netty Project
 *
 * The Netty Project licenses this file to you under the Apache License,
 * version 2.0 (the "License"); you may not use this file except in compliance
 * with the License. You may obtain a copy of the License at:
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations
 * under the License.
 *
 * Copyright (c) The DotNetty Project (Microsoft). All rights reserved.
 *
 *   https://github.com/azure/dotnetty
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 *
 * Copyright (c) 2020 The Dotnetty-Span-Fork Project (cuteant@outlook.com) All rights reserved.
 *
 *   https://github.com/cuteant/dotnetty-span-fork
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

namespace DotNetty.Handlers.Tls
{
    using System;
    using System.Diagnostics;
    using System.Net.Security;
    using System.Threading.Tasks;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;
#if NETCOREAPP_2_0_GREATER || NETSTANDARD_2_0_GREATER
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
#endif

    partial class TlsHandler
    {
        private static readonly Action<Task, object> s_handshakeCompletionCallback = (t, s) => HandleHandshakeCompleted(t, s);
        public static readonly AttributeKey<SslStream> SslStreamAttrKey = AttributeKey<SslStream>.ValueOf("SSLSTREAM");

        private bool EnsureAuthenticated(IChannelHandlerContext ctx)
        {
            var oldState = State;
            if (!oldState.HasAny(TlsHandlerState.AuthenticationStarted))
            {
                State = oldState | TlsHandlerState.Authenticating;
                if (_isServer)
                {
#if NETCOREAPP_2_0_GREATER || NETSTANDARD_2_0_GREATER
                    // Adapt to the SslStream signature
                    ServerCertificateSelectionCallback selector = null;
                    if (_serverCertificateSelector is object)
                    {
                        X509Certificate LocalServerCertificateSelection(object sender, string name)
                        {
                            ctx.GetAttribute(SslStreamAttrKey).Set(_sslStream);
                            return _serverCertificateSelector(ctx, name);
                        }
                        selector = new ServerCertificateSelectionCallback(LocalServerCertificateSelection);
                    }

                    var sslOptions = new SslServerAuthenticationOptions()
                    {
                        ServerCertificate = _serverCertificate,
                        ServerCertificateSelectionCallback = selector,
                        ClientCertificateRequired = _serverSettings.NegotiateClientCertificate,
                        EnabledSslProtocols = _serverSettings.EnabledProtocols,
                        CertificateRevocationCheckMode = _serverSettings.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                        ApplicationProtocols = _serverSettings.ApplicationProtocols // ?? new List<SslApplicationProtocol>()
                    };
                    if (_hasHttp2Protocol)
                    {
                        // https://tools.ietf.org/html/rfc7540#section-9.2.1
                        sslOptions.AllowRenegotiation = false;
                    }
                    _sslStream.AuthenticateAsServerAsync(sslOptions, CancellationToken.None)
                              .ContinueWith(s_handshakeCompletionCallback, this, TaskContinuationOptions.ExecuteSynchronously);
#else
                    _sslStream.AuthenticateAsServerAsync(_serverCertificate,
                                                         _serverSettings.NegotiateClientCertificate,
                                                         _serverSettings.EnabledProtocols,
                                                         _serverSettings.CheckCertificateRevocation)
                              .ContinueWith(s_handshakeCompletionCallback, this, TaskContinuationOptions.ExecuteSynchronously);
#endif
                }
                else
                {
#if NETCOREAPP_2_0_GREATER || NETSTANDARD_2_0_GREATER
                    LocalCertificateSelectionCallback selector = null;
                    if (_userCertSelector is object)
                    {
                        X509Certificate LocalCertificateSelection(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
                        {
                            ctx.GetAttribute(SslStreamAttrKey).Set(_sslStream);
                            return _userCertSelector(ctx, targetHost, localCertificates, remoteCertificate, acceptableIssuers);
                        }
                        selector = new LocalCertificateSelectionCallback(LocalCertificateSelection);
                    }
                    var sslOptions = new SslClientAuthenticationOptions()
                    {
                        TargetHost = _clientSettings.TargetHost,
                        ClientCertificates = _clientSettings.X509CertificateCollection,
                        EnabledSslProtocols = _clientSettings.EnabledProtocols,
                        CertificateRevocationCheckMode = _clientSettings.CheckCertificateRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck,
                        LocalCertificateSelectionCallback = selector,
                        ApplicationProtocols = _clientSettings.ApplicationProtocols
                    };
                    if (_hasHttp2Protocol)
                    {
                        // https://tools.ietf.org/html/rfc7540#section-9.2.1
                        sslOptions.AllowRenegotiation = false;
                    }
                    _sslStream.AuthenticateAsClientAsync(sslOptions, CancellationToken.None)
                              .ContinueWith(s_handshakeCompletionCallback, this, TaskContinuationOptions.ExecuteSynchronously);
#else
                    _sslStream.AuthenticateAsClientAsync(_clientSettings.TargetHost,
                                                         _clientSettings.X509CertificateCollection,
                                                         _clientSettings.EnabledProtocols,
                                                         _clientSettings.CheckCertificateRevocation)
                              .ContinueWith(s_handshakeCompletionCallback, this, TaskContinuationOptions.ExecuteSynchronously);
#endif
                }
                return false;
            }

            return oldState.Has(TlsHandlerState.Authenticated);
        }

        private static void HandleHandshakeCompleted(Task task, object state)
        {
            var self = (TlsHandler)state;
            var oldState = self.State;

            if (task.IsSuccess())
            {
                Debug.Assert(!oldState.HasAny(TlsHandlerState.AuthenticationCompleted));
                self.State = (oldState | TlsHandlerState.Authenticated) & ~(TlsHandlerState.Authenticating | TlsHandlerState.FlushedBeforeHandshake);

                var capturedContext = self.CapturedContext;
                _ = capturedContext.FireUserEventTriggered(TlsHandshakeCompletionEvent.Success);

                if (oldState.Has(TlsHandlerState.ReadRequestedBeforeAuthenticated) && !capturedContext.Channel.Configuration.IsAutoRead)
                {
                    _ = capturedContext.Read();
                }

                if (oldState.Has(TlsHandlerState.FlushedBeforeHandshake))
                {
                    self.Wrap(capturedContext);
                    _ = capturedContext.Flush();
                }
            }
            else if (task.IsCanceled || task.IsFaulted)
            {
                Debug.Assert(!oldState.HasAny(TlsHandlerState.Authenticated));
                self.HandleFailure(task.Exception);
            }
        }

        private void NotifyHandshakeFailure(Exception cause, bool notify)
        {
            var oldState = State;
            if (oldState.HasAny(TlsHandlerState.AuthenticationCompleted)) { return; }

            // handshake was not completed yet => TlsHandler react to failure by closing the channel
            State = (oldState | TlsHandlerState.FailedAuthentication) & ~TlsHandlerState.Authenticating;
            var capturedContext = CapturedContext;
            if (notify)
            {
                _ = capturedContext.FireUserEventTriggered(new TlsHandshakeCompletionEvent(cause));
            }
            this.Close(capturedContext, capturedContext.NewPromise());
        }
    }
}
