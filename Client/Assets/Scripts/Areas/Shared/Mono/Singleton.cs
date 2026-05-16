namespace Assets.Scripts.Areas.Shared.Mono
{
	public abstract class Singleton<T> where T : Singleton<T>, new()
	{
		private static readonly T _instance = new T();
		
		public static T Instance
        {
            get
            {
                return _instance;
            }
        }

		protected Singleton()
        {

        }
	}
}