using UnityEngine;

namespace Printer
{
	public class PrintRefManager 
	{
		private static PrintRefManager _instance;
		
		public static PrintRefManager Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new PrintRefManager();
				}
				return _instance;
			}
		}
		
		private readonly Sprite[] _sprites;
		private int _lastRandomIndex = -1;

		public int Count => _sprites.Length;
		public Sprite this[int index] => _sprites[index];

		public PrintRefManager()
		{
			_sprites = Resources.LoadAll<Sprite>("PrintRefs");
		}

		public Sprite GetRandom()
		{
			if (_sprites.Length == 0) return null;
			if (_sprites.Length == 1) return _sprites[0];

			int index;
			do
			{
				index = Random.Range(0, _sprites.Length);
			} while (index == _lastRandomIndex);

			_lastRandomIndex = index;
			return _sprites[index];
		}
	}
}