using Game.Features.Locations;
using UnityEngine;
using VContainer;

namespace InputSystem
{
    public sealed class LocationObjectTapCommand : Command<ILocationInteractable>, ITapInputHandler
    {
        private LocationInteractionRouter _router;

        public bool IsOverWorldCanvas => false;

        [Inject]
        public void Install(LocationInteractionRouter router)
        {
            _router = router;
        }

        public void OnTap(Vector3 position)
        {
            _router?.Route(Target, position);
        }

        public void OnPointerDown()
        {
        }

        public void InPointerUp()
        {
        }

        public void OnCancel()
        {
        }

        protected override bool IsValid(ILocationInteractable obj, object param = null)
        {
            return obj != null && obj.IsInteractionEnabled;
        }
    }
}
