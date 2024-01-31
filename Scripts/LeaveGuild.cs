using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Game;
using UnityEngine;
using DaggerfallWorkshop.Game.UserInterfaceWindows;

namespace Berzeger
{
    public class LeaveGuild : MonoBehaviour
    {
        private static Mod _mod;

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            _mod = initParams.Mod;
            var go = new GameObject(_mod.Title);
            go.AddComponent<LeaveGuild>();
        }

        void Awake()
        {
            //var settings = _mod.GetSettings();
            // ---
            UIWindowFactory.RegisterCustomUIWindow(UIWindowType.GuildServicePopup, typeof(GuildServicePopupWindow));
            _mod.IsReady = true;
        }
    }
}