using System;

public class Grid
{
    //  Variables
    //  ---------

	public int width;
	public int height;
	public int[] values;

    //  Properties
    //  ----------

	public int Size => width * height;

    //  Functions
    //  ---------

	public Grid(int width, int height, int value)
	{
		this.width = width;
		this.height = height;

		values = new int[Size];
		for (int i = 0; i < Size; ++i)
			values[i] = value;
	}

	public bool Contains(int i, int j)
	{
		return i >= 0 && i < width && j >= 0 && j < height;
	}

    public void Set(int value, int i, int j)
    {
        values[i + j * width] = value;
    }

    public int Get(int i)
    {
        return values[i];
    }

    public bool IsValue(int value, int i, int j)
    {
        int index = i + j * width;
        return (index < values.Length) && (values[index] & value) > 0;
    }

    public void Clear()
    {
        Array.Clear(values, 0, values.Length);
    }
}
