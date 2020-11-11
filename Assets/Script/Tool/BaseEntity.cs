using UnityEngine;

namespace Tool
{
    public class BaseEntity : MonoBehaviour
    {
        [SerializeField] private string _EntityName;

        protected Transform _Trans;

        public string EntityName
        {
            set => _EntityName = value;

            get => string.IsNullOrEmpty(_EntityName) ? name : _EntityName;
        }

        public Transform Trans => _Trans;

        protected virtual void Awake()
        {
            _Trans = transform;
        }
    }
}