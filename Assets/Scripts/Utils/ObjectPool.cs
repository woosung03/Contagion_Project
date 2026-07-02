using System.Collections.Generic;
using UnityEngine;

namespace Contagion.Utils
{
    /// <summary>
    /// 범용 컴포넌트 오브젝트 풀. 설계 문서 15절 "DNA 버블은 오브젝트 풀링 필수" 요구사항 대응.
    /// BubbleSpawner 등에서 사용한다.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Stack<T> _inactive = new Stack<T>();

        public ObjectPool(T prefab, Transform parent, int prewarmCount = 0)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < prewarmCount; i++)
            {
                var instance = CreateInstance();
                Release(instance);
            }
        }

        private T CreateInstance()
        {
            var instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        public T Get()
        {
            T instance = _inactive.Count > 0 ? _inactive.Pop() : CreateInstance();
            instance.gameObject.SetActive(true);
            return instance;
        }

        public void Release(T instance)
        {
            instance.gameObject.SetActive(false);
            _inactive.Push(instance);
        }
    }
}
