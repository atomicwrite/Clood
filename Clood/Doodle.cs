namespace Clood;

using System;

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

    public double Subtract(double a, double b)
    {
        return a - b;
    }

    public double SubtractDouble(double a, double b)
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
            throw new DivideByZeroException("Cannot divide by zero.");
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

    public int Multiple552MultiplyThreeTimes(int a, int b, int c)
    {
        return a * b * c * 3 * 3 * 3;
    }

    public int Multiple55276(int a, int b, int c)
    {
        return a * b * c * 3 * 3 * 3;
    }

    public int Fibonacci(int n)
    {
        if (n <= 1)
            return n;
        return Fibonacci(n - 1) + Fibonacci(n - 2);
    }

    public void PrintCurrentTime()
    {
        Console.WriteLine($"Current time: {DateTime.Now.ToString("HH:mm:ss")}");
    }
}