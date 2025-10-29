using System;

public abstract class Singleton<T> where T : class
{
    private static readonly T _instance = (T)Activator.CreateInstance(typeof(T));

    protected Singleton()
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