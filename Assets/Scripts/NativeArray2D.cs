using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Internal;


[DebuggerDisplay("Length0 = {Length0}, Length1 = {Length1}")]
[DebuggerTypeProxy(typeof(NativeArray2DDebugView<>))]
    
[NativeContainer]
[NativeContainerSupportsDeallocateOnJobCompletion]
public unsafe struct NativeArray2D<T>
    : IDisposable
    , IEnumerable<T>
    , IEquatable<NativeArray2D<T>>

#if CSHARP_7_3_OR_NEWER
    where T : unmanaged
#else
	where T : struct
#endif
{
    [ExcludeFromDocs]
    public struct Enumerator : IEnumerator<T>
    {
        private NativeArray2D<T> m_Array;

        private int m_Index0;
        private int m_Index1;

        public Enumerator(ref NativeArray2D<T> array)
        {
            m_Array = array;
            m_Index0 = -1;
            m_Index1 = 0;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            m_Index0++;
            if (m_Index0 >= m_Array.Length0)
            {
                m_Index0 = 0;
                m_Index1++;
                return m_Index1 < m_Array.Length1;
            }
            return true;
        }

        public void Reset()
        {
            m_Index0 = -1;
            m_Index1 = 0;
        }

        public T Current
        {
            get
            {
                return m_Array[m_Index0, m_Index1];
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }
    }

    [NativeDisableUnsafePtrRestriction]
    private void* m_Buffer;

    private int m_Length0;

    private int m_Length1;

    // These fields are all required when safety checks are enabled
    // They must have these exact types, names, and order
#if ENABLE_UNITY_COLLECTIONS_CHECKS

    private AtomicSafetyHandle m_Safety;

    [NativeSetClassTypeToNullOnSchedule]
    private DisposeSentinel m_DisposeSentinel;
#endif

    internal Allocator m_Allocator;

    public NativeArray2D(
        int length0,
        int length1,
        Allocator allocator,
        NativeArrayOptions options = NativeArrayOptions.ClearMemory)
    {
        Allocate(length0, length1, allocator, out this);
        if ((options & NativeArrayOptions.ClearMemory)
            == NativeArrayOptions.ClearMemory)
        {
            UnsafeUtility.MemClear(
                m_Buffer,
                Length * (long)UnsafeUtility.SizeOf<T>());
        }
    }

    public NativeArray2D(T[,] array, Allocator allocator)
    {
        int length0 = array.GetLength(0);
        int length1 = array.GetLength(1);
        Allocate(length0, length1, allocator, out this);
        Copy(array, this);
    }

    public NativeArray2D(NativeArray2D<T> array, Allocator allocator)
    {
        Allocate(array.Length0, array.Length1, allocator, out this);
        Copy(array, this);
    }

    public int Length
    {
        get
        {
            return m_Length0 * m_Length1;
        }
    }

    public int Length0
    {
        get
        {
            return m_Length0;
        }
    }

    public int Length1
    {
        get
        {
            return m_Length1;
        }
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private void RequireReadAccess()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private void RequireWriteAccess()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private void RequireIndexInBounds(int index0, int index1)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (index0 < 0 || index0 >= m_Length0)
        {
            throw new IndexOutOfRangeException();
        }
        if (index1 < 0 || index1 >= m_Length1)
        {
            throw new IndexOutOfRangeException();
        }
#endif
    }

    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    private static void RequireValidAllocator(Allocator allocator)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (!UnsafeUtility.IsValidAllocator(allocator))
        {
            throw new InvalidOperationException(
                "The NativeArray2D can not be Disposed because it was " +
                "not allocated with a valid allocator.");
        }
#endif
    }

    public T this[int index0, int index1]
    {
        get
        {
            RequireReadAccess();
            RequireIndexInBounds(index0, index1);

            int index = index1 * m_Length0 + index0;
            return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
        }

        [WriteAccessRequired]
        set
        {
            RequireWriteAccess();
            RequireIndexInBounds(index0, index1);

            int index = index1 * m_Length0 + index0;
            UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
        }
    }

    public bool IsCreated
    {
        get
        {
            return (IntPtr)m_Buffer != IntPtr.Zero;
        }
    }

    [WriteAccessRequired]
    public void Dispose()
    {
        RequireWriteAccess();
        RequireValidAllocator(m_Allocator);

        // Make sure we're not double-disposing
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if UNITY_2018_3_OR_NEWER
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#else
		DisposeSentinel.Dispose(m_Safety, ref m_DisposeSentinel);
#endif
#endif

        UnsafeUtility.Free(m_Buffer, m_Allocator);
        m_Buffer = null;
        m_Length0 = 0;
        m_Length1 = 0;
    }

    [WriteAccessRequired]
    public void CopyFrom(T[,] array)
    {
        Copy(array, this);
    }

    [WriteAccessRequired]
    public void CopyFrom(NativeArray2D<T> array)
    {
        Copy(array, this);
    }

    public void CopyTo(T[,] array)
    {
        Copy(this, array);
    }

    public void CopyTo(NativeArray2D<T> array)
    {
        Copy(this, array);
    }

    public T[,] ToArray()
    {
        T[,] dst = new T[m_Length0, m_Length1];
        Copy(this, dst);
        return dst;
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(ref this);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return new Enumerator(ref this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Equals(NativeArray2D<T> other)
    {
        return m_Buffer == other.m_Buffer
                && m_Length0 == other.m_Length0
                && m_Length1 == other.m_Length1;
    }

    public override bool Equals(object other)
    {
        if (other is null)
        {
            return false;
        }
        return other is NativeArray2D<T> && Equals((NativeArray2D<T>)other);
    }

    public override int GetHashCode()
    {
        int result = (int)m_Buffer;
        result = (result * 397) ^ m_Length0;
        result = (result * 397) ^ m_Length1;
        return result;
    }

    public static bool operator ==(NativeArray2D<T> a, NativeArray2D<T> b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(NativeArray2D<T> a, NativeArray2D<T> b)
    {
        return !a.Equals(b);
    }

    private static void Allocate(
        int length0,
        int length1,
        Allocator allocator,
        out NativeArray2D<T> array)
    {
        RequireValidAllocator(allocator);

#if !CSHARP_7_3_OR_NEWER
        if (!UnsafeUtility.IsUnmanaged<T>())
        {
            throw new InvalidOperationException(
                "Only unmanaged types are supported");
        }
#endif

        int length = length0 * length1;
        if (length <= 0)
        {
            throw new InvalidOperationException(
                "Total number of elements must be greater than zero");
        }

        array = new NativeArray2D<T>
        {
            m_Buffer = UnsafeUtility.Malloc(
                length * UnsafeUtility.SizeOf<T>(),
                UnsafeUtility.AlignOf<T>(),
                allocator),
            m_Length0 = length0,
            m_Length1 = length1,
            m_Allocator = allocator
        };
        DisposeSentinel.Create(
            out array.m_Safety,
            out array.m_DisposeSentinel,
            1,
            allocator);
    }

    private static void Copy(NativeArray2D<T> src, NativeArray2D<T> dest)
    {
        src.RequireReadAccess();
        dest.RequireWriteAccess();

        if (src.Length0 != dest.Length0
            || src.Length1 != dest.Length1)
        {
            throw new ArgumentException("Arrays must have the same size");
        }

        for (int index0 = 0; index0 < src.Length0; ++index0)
        {
            for (int index1 = 0; index1 < src.Length1; ++index1)
            {
                dest[index0, index1] = src[index0, index1];
            }
        }
    }

    private static void Copy(T[,] src, NativeArray2D<T> dest)
    {
        dest.RequireWriteAccess();

        if (src.GetLength(0) != dest.Length0
            || src.GetLength(1) != dest.Length1)
        {
            throw new ArgumentException("Arrays must have the same size");
        }

        for (int index0 = 0; index0 < dest.Length0; ++index0)
        {
            for (int index1 = 0; index1 < dest.Length1; ++index1)
            {
                dest[index0, index1] = src[index0, index1];
            }
        }
    }

    private static void Copy(NativeArray2D<T> src, T[,] dest)
    {
        src.RequireReadAccess();

        if (src.Length0 != dest.GetLength(0)
            || src.Length1 != dest.GetLength(1))
        {
            throw new ArgumentException("Arrays must have the same size");
        }

        for (int index0 = 0; index0 < src.Length0; ++index0)
        {
            for (int index1 = 0; index1 < src.Length1; ++index1)
            {
                dest[index0, index1] = src[index0, index1];
            }
        }
    }
}

internal sealed class NativeArray2DDebugView<T>
#if CSHARP_7_3_OR_NEWER
    where T : unmanaged
#else
	where T : struct
#endif
{

    private readonly NativeArray2D<T> m_Array;


    public NativeArray2DDebugView(NativeArray2D<T> array)
    {
        m_Array = array;
    }

    public T[,] Items
    {
        get
        {
            return m_Array.ToArray();
        }
    }
}
