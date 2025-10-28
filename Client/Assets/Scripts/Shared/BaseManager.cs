using System;
using Cysharp.Threading.Tasks;

public abstract class BaseManager<T> where T : class
{
    private readonly string _baseUrl = "https://localhost:5001";

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

    protected UniTask<T> RequestPost()
        where T : class
    {

    }
}