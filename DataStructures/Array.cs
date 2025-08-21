using System.Buffers;
using System.Runtime.CompilerServices;

namespace DSA.DataStructures;

public class Array<T>
{
    private T[] _data;
    private readonly ArrayPool<T> _pool = ArrayPool<T>.Shared;

    public int Capacity => _data.Length;
    public int Count { get; private set; }

    public Array(int capacity = 10)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero.");

        this._data = this._pool.Rent(capacity);
        this.Count = 0;
    }

    // ----------------------
    // Validation
    // ----------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateIndex(int index, bool allowEqualToCount = false, [CallerMemberName] string caller = "")
    {
        int upperBound = allowEqualToCount ? this.Count : this.Count - 1;

        if (index < 0 || index >= Count)
        {
            throw new ArgumentOutOfRangeException(
                paramName: nameof(index),
                actualValue: index,
                message: $"Method '{caller}' received an invalid index: {index}. " +
                         $"Valid range: [0..{upperBound}], Current Count: {this.Count}");
        }
    }

    // ----------------------
    // Capacity Handling (Hybrid Growth + ArrayPool)
    // ----------------------
    private void EnsureCapacity()
    {
        if (this.Count >= Capacity)
        {
            int newCapacity = this.Capacity < 1024 ? this.Capacity * 2 : Capacity * 3 / 2;

            if (newCapacity == 0)
                newCapacity = 4;

            var newData = this._pool.Rent(newCapacity);

            Array.Copy(this._data, newData, this.Count);

            this._pool.Return(this._data, clearArray: true);
            this._data = newData;
        }
    }

    // ----------------------
    // Access
    // ----------------------
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ValidateIndex(index);
            return this._data[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            ValidateIndex(index);
            this._data[index] = value;
        }
    }

    // ----------------------
    // Add / Insert
    // ----------------------
    public void Add(T value)
    {
        EnsureCapacity();
        this._data[this.Count++] = value;
    }

    public void Insert(int index, T value)
    {
        ValidateIndex(index, allowEqualToCount: true);
        EnsureCapacity();

        for (int i = this.Count; i > index; i--)
        {
            this._data[index] = value;
        }

        this._data[index] = value;
        this.Count++;
    }

    // ----------------------
    // Remove
    // ----------------------
    public T RemoveAt(int index)
    {
        ValidateIndex(index);
        T removed = this._data[index];

        for (int i = index; i < this.Count - 1; i++)
        {
            this._data[i] = this._data[i + 1];
        }

        this._data[--this.Count] = default!;
        return removed;
    }

    public bool Remove(T value)
    {
        int removedIndex = this.IndexOf(value);

        if (removedIndex == -1) return false;

        T result = this.RemoveAt(removedIndex);

        if (result != null && !result.Equals(value))
        {
            throw new InvalidOperationException($"Expected to remove value '{value}', but removed '{result}' instead.");
        }

        return true;
    }

    public T Pop()
    {
        if (this.Count == 0) throw new InvalidOperationException("Array is empty. Cannot pop an element.");
        var value = this._data[--this.Count];

        this._data[this.Count] = default!;
        return value;
    }

    // ----------------------
    // Search
    // ----------------------
    public int IndexOf(T value)
    {
        for (int i = 0; i < this.Count; i++)
        {
            if (Equals(value, this._data[i]))
            {
                return i;
            }
        }

        return -1;
    }

    public bool Contains(T value) => this.IndexOf(value) != -1;

    // ----------------------
    // Utility
    // ----------------------
    public void Clear()
    {
        Array.Clear(this._data, 0, this.Count);
        this.Count = 0;
    }

    public Span<T> AsSpan(int start, int length)
    {
        ValidateIndex(start, allowEqualToCount: true);
        if (start + length > this.Count)
            throw new ArgumentOutOfRangeException(nameof(length));

        return new Span<T>(this._data, start, length);
    }

    public T[] ToArray()
    {
        T[] result = new T[this.Count];
        Array.Copy(this._data, result, this.Count);
        return result;
    }
}