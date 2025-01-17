﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FASTER.core
{
    public unsafe partial class FasterKV<Key, Value> : FasterBase, IFasterKV<Key, Value>
    {
        /// <summary>
        /// Manual Lock operation. Locks the record corresponding to 'key'.
        /// </summary>
        /// <param name="key">key of the record.</param>
        /// <param name="lockOp">Lock operation being done.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OperationStatus InternalLock(ref Key key, LockOperation lockOp)
        {
            Debug.Assert(epoch.ThisInstanceProtected(), "InternalLock must have protected epoch");
            Debug.Assert(this.LockTable.IsEnabled, "ManualLockTable must be enabled for InternalLock");

            OperationStackContext<Key, Value> stackCtx = new(comparer.GetHashCode64(ref key));
            FindTag(ref stackCtx.hei);
            stackCtx.SetRecordSourceToHashEntry(hlog);

            switch (lockOp.LockOperationType)
            {
                case LockOperationType.Lock:
                    if (!this.LockTable.TryLockManual(ref key, ref stackCtx.hei, lockOp.LockType))
                        return OperationStatus.RETRY_LATER;
                    return OperationStatus.SUCCESS;
                case LockOperationType.Unlock:
                    this.LockTable.Unlock(ref key, ref stackCtx.hei, lockOp.LockType);
                    return OperationStatus.SUCCESS;
                default:
                    Debug.Fail($"Unexpected {nameof(LockOperationType)}: {lockOp.LockOperationType}");
                    break;
            }
            return OperationStatus.SUCCESS;
        }

        /// <summary>
        /// Manual Lock operation for <see cref="HashBucket"/> locking . Locks the buckets corresponding to 'keys'.
        /// </summary>
        /// <param name="keyLockCode">Lock code of the key (<see cref="HashBucket"/>) to be locked or unlocked.</param>
        /// <param name="lockOp">Lock operation being done.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal OperationStatus InternalLock(long keyLockCode, LockOperation lockOp)
        {
            Debug.Assert(epoch.ThisInstanceProtected(), "InternalLock must have protected epoch");
            Debug.Assert(this.LockTable.IsEnabled, "ManualLockTable must be enabled for InternalLock");

            switch (lockOp.LockOperationType)
            {
                case LockOperationType.Lock:
                    if (!this.LockTable.TryLockManual(keyLockCode, lockOp.LockType))
                        return OperationStatus.RETRY_LATER;
                    return OperationStatus.SUCCESS;
                case LockOperationType.Unlock:
                    this.LockTable.Unlock(keyLockCode, lockOp.LockType);
                    return OperationStatus.SUCCESS;
                default:
                    Debug.Fail($"Unexpected {nameof(LockOperationType)}: {lockOp.LockOperationType}");
                    break;
            }
            return OperationStatus.SUCCESS;
        }
    }
}
