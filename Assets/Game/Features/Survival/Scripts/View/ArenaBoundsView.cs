using Unity.Entities;
using UnityEngine;

namespace Survival
{
    // Runtime visual of the baked ArenaBounds rectangle, drawn with a LineRenderer
    // so the playable area is visible in the Game view. Place on a GameObject in
    // the normal scene (not the SubScene). Configure width / material / color on
    // the LineRenderer component itself.
    [RequireComponent(typeof(LineRenderer))]
    public class ArenaBoundsView : MonoBehaviour
    {
        [SerializeField] private float _y;

        private LineRenderer _line;
        private EntityQuery _query;
        private bool _hasQuery;
        private bool _drawn;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.loop = true;
            _line.useWorldSpace = true;
            _line.positionCount = 4;
        }

        private void Update()
        {
            if (_drawn)
                return;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null)
                return;

            if (!_hasQuery)
            {
                _query = world.EntityManager.CreateEntityQuery(typeof(ArenaBounds));
                _hasQuery = true;
            }

            // Wait until the SubScene has baked and streamed the bounds entity.
            if (_query.CalculateEntityCount() != 1)
                return;

            var bounds = _query.GetSingleton<ArenaBounds>();
            _line.SetPositions(new[]
            {
                new Vector3(bounds.Min.x, _y, bounds.Min.y),
                new Vector3(bounds.Min.x, _y, bounds.Max.y),
                new Vector3(bounds.Max.x, _y, bounds.Max.y),
                new Vector3(bounds.Max.x, _y, bounds.Min.y),
            });
            _drawn = true;
        }
    }
}
