using System;
using System.Collections.Generic;

// For holding a set of <see cref="Guid"/> as identifiers with a set of hashes. Enabling sorting and finding collisions.
public sealed class IdHashBuffer
{
    Guid[] ids;
    ulong[] hashes;
    int offset;
    bool seal;

    public IdHashBuffer(int size)
    {
        Size = size;
        ids = new Guid[size];
        hashes = new ulong[size];
    }

    public int Size { get; }

    /// <summary>
    /// Sorts the structure, sealing it from further <see cref="TryWrite"/> operations, enabling <see cref="FindHashCollisions"/>.
    /// </summary>
    public void Seal()
    {
        seal = true;
        Array.Sort(hashes, ids, 0, offset);
    }

    public bool TryWrite(Guid id, ulong hash)
    {
        if (seal)
        {
            throw new InvalidOperationException("Cannot add to a sealed buffer.");
        }
        if (offset == Size)
        {
            return false;
        }

        ids[offset] = id;
        hashes[offset] = hash;
        offset += 1;
        return true;
    }

    public void FindHashCollisions(IEnumerable<IdHashBuffer> buffers, Action<ulong, ArraySegment<Guid>> onCollision)
    {
        if (seal == false)
        {
            throw new InvalidOperationException("Cannot search in a not sealed buffer.");
        }

        var guids = new Guid[1024];

        foreach (var buffer in buffers)
        {
            for (var i = 0; i < offset; i++)
            {
                var hash = hashes[i];
                var id = ids[i];

                var collisions = buffer.FindIdsByHash(hash, guids, id);
                if (collisions > 0)
                {
                    guids[collisions] = id;
                    onCollision(hash, new ArraySegment<Guid>(guids, 0, collisions + 1));
                }
            }
        }
    }

    int FindIdsByHash(ulong hash, Guid[] resultIds, Guid skippedIdentifier)
    {
        var index = Array.BinarySearch(hashes, 0, offset, hash);
        if (index < 0)
        {
            return 0;
        }

        // go left to find all
        while (index - 1 >= 0 && hashes[index - 1] == hash)
        {
            index -= 1;
        }

        // copy all
        var i = 0;
        var realCollisions = 0;
        while (index + i < offset && hashes[index + i] == hash && i < resultIds.Length)
        {
            var possiblyColliding = ids[index + i];

            if (possiblyColliding != skippedIdentifier)
            {
                resultIds[realCollisions] = possiblyColliding;
                realCollisions += 1;
            }
            i += 1;
        }

        return realCollisions;
    }
}