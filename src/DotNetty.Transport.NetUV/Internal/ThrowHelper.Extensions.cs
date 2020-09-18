/*
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
 * Copyright (c) 2020 The Dotnetty-Span-Fork Project (cuteant@outlook.com) All rights reserved.
 *
 *   https://github.com/cuteant/dotnetty-span-fork
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;
using DotNetty.NetUV.Native;
using DotNetty.Transport.Channels;

namespace DotNetty.Transport.Libuv
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        s,

        pi,
        fi,
        ts,

        asm,
        key,
        obj,
        str,

        list,
        pool,
        name,
        path,
        item,
        type,
        func,
        task,

        match,
        array,
        other,
        inner,
        types,
        value,
        index,
        count,

        policy,
        offset,
        method,
        buffer,
        source,
        values,
        parent,
        length,
        target,
        member,

        feature,
        manager,
        newSize,
        invoker,
        options,

        assembly,
        capacity,
        fullName,
        typeInfo,
        typeName,
        nThreads,
        pipeName,

        defaultFn,
        fieldInfo,
        predicate,

        memberInfo,
        returnType,
        collection,
        expression,
        startIndex,

        directories,
        dirEnumArgs,
        destination,

        valueFactory,
        propertyInfo,
        instanceType,

        attributeType,

        chooserFactory,
        eventLoopGroup,
        parameterTypes,

        assemblyPredicate,
        qualifiedTypeName,

        includedAssemblies,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
    }

    #endregion

    internal partial class ThrowHelper
    {
        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Task FromArgumentException_RegChannel()
        {
            return TaskUtil.FromException(GetArgumentException());

            static ArgumentException GetArgumentException()
            {
                return new ArgumentException($"channel must be of {typeof(INativeChannel)}");
            }
        }

        #endregion

        #region -- InvalidOperationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Task FromInvalidOperationException(IntPtr loopHandle)
        {
            return TaskUtil.FromException(GetInvalidOperationException());
            InvalidOperationException GetInvalidOperationException()
            {
                return new InvalidOperationException($"Loop {loopHandle} does not exist");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_ExecutionState(int executionState)
        {
            throw GetInvalidOperationException();
            InvalidOperationException GetInvalidOperationException()
            {
                return new InvalidOperationException($"Invalid state {executionState}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_ExecutionState0(int executionState)
        {
            throw GetInvalidOperationException();
            InvalidOperationException GetInvalidOperationException()
            {
                return new InvalidOperationException($"Invalid {nameof(AbstractUVEventLoop)} state {executionState}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_Dispatch()
        {
            throw GetInvalidOperationException();

            static InvalidOperationException GetInvalidOperationException()
            {
                return new InvalidOperationException("No pipe connections to dispatch handles.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static uint FromInvalidOperationException_Dispatch(AddressFamily addressFamily)
        {
            throw GetInvalidOperationException();
            InvalidOperationException GetInvalidOperationException()
            {
                return new InvalidOperationException($"Address family : {addressFamily} platform : {RuntimeInformation.OSDescription} not supported");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CreateChild(Exception ex)
        {
            throw GetInvalidOperationException();
            InvalidOperationException GetInvalidOperationException()
            {
                return new InvalidOperationException($"Failed to create a child {nameof(WorkerEventLoop)}.", ex.Unwrap());
            }
        }

        #endregion

        #region -- ChannelException --

        internal static ChannelException GetChannelException(OperationException ex)
        {
            return new ChannelException(ex);
        }

        internal static ChannelException GetChannelException_FailedToWrite(OperationException error)
        {
            return new ChannelException("Failed to write", error);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowChannelException(Exception exc)
        {
            throw GetChannelException();
            ChannelException GetChannelException()
            {
                return new ChannelException(exc);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowChannelException(ChannelOption option)
        {
            throw GetChannelException();
            ChannelException GetChannelException()
            {
                return new ChannelException($"Invalid channel option {option}");
            }
        }

        #endregion

        #region -- SocketException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSocketException(int errorCode)
        {
            throw GetSocketException();
            SocketException GetSocketException()
            {
                return new SocketException(errorCode);
            }
        }

        #endregion

        #region -- TimeoutException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException(string pipeName)
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException($"Connect to dispatcher pipe {pipeName} timed out.");
            }
        }

        #endregion
    }
}
