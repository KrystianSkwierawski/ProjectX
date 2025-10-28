using System;

public abstract class BaseManager<T> where T : class
{
    private static readonly T _instance = (T)Activator.CreateInstance(typeof(T));

    protected BaseManager()
    {

    }

    public static T Instance
    {
        get
        {
            return _instance;
        }
    }
}