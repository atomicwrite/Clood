namespace Clood;

public class Doodle
{
    public int Add(int a, int b)
    {
        return a + b;
    }

    public int Subtract(int a, int b)
    {
        return a - b;
    }

    public int Multiply(int a, int b)
    {
        return a * b;
    }

    public int Divide(int a, int b)
    {
        if (b == 0)
        {
            throw new System.DivideByZeroException("Cannot divide by zero.");
        }
        return a / b;
    }

    public int Multiple52(int a, int b, int c)
    {
        return a * b * c;
    }

    public int Multiple552(int a, int b, int c)
    {
        return a * b * c * 3;
    }

    public int Multiple552ThreeTimes(int a, int b, int c)
    {
        return a * b * c * 3 * 3 * 3;
    }
}