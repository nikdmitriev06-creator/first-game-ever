using UnityEngine;

/// <summary>
/// Slot storage with no knowledge of rooms, scenes, or widgets.
/// </summary>
public sealed class ItemContainer
{
    readonly string[] _slots;
    readonly int[] _counts;

    public ItemContainer(int slotCount)
    {
        int count = Mathf.Max(1, slotCount);
        _slots = new string[count];
        _counts = new int[count];
    }

    public int SlotCount => _slots.Length;

    public string IdAt(int index)
    {
        if ((uint)index >= (uint)_slots.Length || _counts[index] <= 0)
            return null;

        return _slots[index];
    }

    public int CountAt(int index)
    {
        if ((uint)index >= (uint)_slots.Length || string.IsNullOrEmpty(_slots[index]))
            return 0;

        return _counts[index];
    }

    public void Set(int index, string id, int count)
    {
        if ((uint)index >= (uint)_slots.Length)
            return;

        if (string.IsNullOrEmpty(id) || count <= 0)
        {
            Clear(index);
            return;
        }

        _slots[index] = id;
        _counts[index] = count;
    }

    public void Clear(int index)
    {
        if ((uint)index >= (uint)_slots.Length)
            return;

        _slots[index] = null;
        _counts[index] = 0;
    }

    public int Add(string id, int count)
    {
        if (string.IsNullOrEmpty(id) || count <= 0)
            return 0;

        int left = count;
        int max = ItemIds.MaxCount(id);
        if (ItemIds.Stacks(id))
        {
            for (int i = 0; i < _slots.Length && left > 0; i++)
            {
                if (_slots[i] != id || _counts[i] <= 0 || _counts[i] >= max)
                    continue;

                int move = Mathf.Min(max - _counts[i], left);
                _counts[i] += move;
                left -= move;
            }
        }

        while (left > 0)
        {
            int slot = FirstEmpty();
            if (slot < 0)
                break;

            int move = Mathf.Min(max, left);
            _slots[slot] = id;
            _counts[slot] = move;
            left -= move;
        }

        return left;
    }

    public int CountOf(string id, string cursor, int cursorCount)
    {
        if (string.IsNullOrEmpty(id))
            return 0;

        int total = 0;
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i] == id)
                total += _counts[i];
        }

        if (cursor == id && cursorCount > 0)
            total += cursorCount;

        return total;
    }

    public bool Remove(string id, int count, ref string cursor, ref int cursorCount)
    {
        if (count <= 0 || string.IsNullOrEmpty(id))
            return true;

        if (CountOf(id, cursor, cursorCount) < count)
            return false;

        int left = count;
        for (int i = 0; i < _slots.Length && left > 0; i++)
        {
            if (_slots[i] != id || _counts[i] <= 0)
                continue;

            int take = Mathf.Min(_counts[i], left);
            _counts[i] -= take;
            left -= take;
            if (_counts[i] <= 0)
                Clear(i);
        }

        if (left > 0 && cursor == id && cursorCount > 0)
        {
            int take = Mathf.Min(cursorCount, left);
            cursorCount -= take;
            left -= take;
            if (cursorCount <= 0)
            {
                cursor = null;
                cursorCount = 0;
            }
        }

        return left <= 0;
    }

    public bool CanFitAfterSpend(string resultId, int resultCount, Ingredient[] spend, string cursor, int cursorCount)
    {
        if (resultCount <= 0 || string.IsNullOrEmpty(resultId))
            return true;

        string[] slots = (string[])_slots.Clone();
        int[] counts = (int[])_counts.Clone();
        if (!TakeAll(slots, counts, ref cursor, ref cursorCount, spend))
            return false;

        return SpaceIn(slots, counts, resultId) >= resultCount;
    }

    public int FirstEmpty()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (string.IsNullOrEmpty(_slots[i]) || _counts[i] <= 0)
                return i;
        }

        return -1;
    }

    public int SpaceFor(string id)
    {
        return SpaceIn(_slots, _counts, id);
    }

    static bool TakeAll(string[] slots, int[] counts, ref string cursor, ref int cursorCount, Ingredient[] spend)
    {
        if (spend == null)
            return true;

        for (int i = 0; i < spend.Length; i++)
        {
            if (!Take(slots, counts, ref cursor, ref cursorCount, spend[i].ItemId, spend[i].Count))
                return false;
        }

        return true;
    }

    static bool Take(string[] slots, int[] counts, ref string cursor, ref int cursorCount, string id, int count)
    {
        if (count <= 0 || string.IsNullOrEmpty(id))
            return true;

        int have = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == id)
                have += counts[i];
        }

        if (cursor == id && cursorCount > 0)
            have += cursorCount;
        if (have < count)
            return false;

        int left = count;
        for (int i = 0; i < slots.Length && left > 0; i++)
        {
            if (slots[i] != id || counts[i] <= 0)
                continue;

            int take = Mathf.Min(counts[i], left);
            counts[i] -= take;
            left -= take;
            if (counts[i] <= 0)
            {
                slots[i] = null;
                counts[i] = 0;
            }
        }

        if (left > 0 && cursor == id && cursorCount > 0)
        {
            int take = Mathf.Min(cursorCount, left);
            cursorCount -= take;
            left -= take;
            if (cursorCount <= 0)
            {
                cursor = null;
                cursorCount = 0;
            }
        }

        return left <= 0;
    }

    static int SpaceIn(string[] slots, int[] counts, string id)
    {
        int space = 0;
        int max = ItemIds.MaxCount(id);
        if (ItemIds.Stacks(id))
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == id && counts[i] > 0)
                    space += Mathf.Max(0, max - counts[i]);
            }
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (string.IsNullOrEmpty(slots[i]) || counts[i] <= 0)
                space += max;
        }

        return space;
    }
}
