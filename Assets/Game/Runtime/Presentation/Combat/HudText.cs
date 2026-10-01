using System.Collections.Generic;
using System.Globalization;

namespace Praxen.Game.Presentation.Combat
{
    public static class HudText
    {
        private static readonly Dictionary<string, string> English = new Dictionary<string, string>
        {
            ["combat.prepare"] = "Prepare to defend",
            ["combat.instructions"] = "Swipe in the shown direction to parry",
            ["combat.opening"] = "OPENING: swipe through the arena to attack",
            ["combat.victory"] = "VICTORY: restart the encounter",
            ["combat.defeat"] = "DEFEATED: restart the encounter",
            ["combat.attack"] = "{0}: {1} damage{2}",
            ["combat.finisher"] = " (FINISHER)",
            ["result.Hit"] = "HIT", ["result.Blocked"] = "BLOCKED",
            ["result.Parried"] = "PARRIED", ["result.Dodged"] = "DODGED",
            ["result.GuardBroken"] = "GUARD BROKEN", ["result.IgnoredAfterDeath"] = "ENCOUNTER ENDED",
            ["direction.Left"] = "Left", ["direction.Right"] = "Right",
            ["direction.Up"] = "Up", ["direction.Down"] = "Down",
            ["tell.guard"] = "GUARD", ["tell.parry"] = "PARRY {0}", ["tell.dodge"] = "DODGE {0}",
            ["dodge.Left"] = "LEFT", ["dodge.Right"] = "RIGHT",
            ["dodge.Both"] = "BOTH", ["dodge.None"] = "NONE",
            ["ability.none"] = "No ability equipped",
            ["title"] = "FOREVER WE REIGN",
            ["guard"] = "GUARD", ["left"] = "LEFT", ["right"] = "RIGHT",
            ["ability"] = "ABILITY", ["pause"] = "MENU", ["resume"] = "RESUME",
            ["restart"] = "RESTART DUEL", ["menu"] = "DUEL PAUSED",
            ["hand.right"] = "RIGHT HAND", ["hand.left"] = "LEFT HAND",
            ["scale.down"] = "SMALLER", ["scale.up"] = "LARGER",
            ["scale"] = "CONTROL SIZE {0:0}%", ["calibrate"] = "CALIBRATE REACH",
            ["explore"] = "EXPLORE FOUNDRY", ["cancel"] = "CANCEL", ["apply"] = "APPLY",
            ["calibration"] = "TAP COMFORTABLY BELOW\n{0}/3 TAPS",
            ["calibration.complete"] = "REACH CAPTURED\nAPPLY OR CANCEL",
            ["resources"] = "HP {0}   GUARD {1}   DODGE {2}/{3}",
            ["state"] = "{0}   {1:0.00}s", ["offense"] = "ENEMY HP {0}   BALANCE {1}\nFOCUS {2}/{3}   {4}{5}",
            ["open"] = "OPEN {0:0.00}s", ["ended"] = "ENCOUNTER ENDED", ["defend"] = "DEFEND",
            ["buffered"] = "\nATTACK BUFFERED", ["countdown"] = "RESUMING IN {0}",
            ["suspended"] = "SUSPENDED: {0}",
            ["reason.None"] = "NONE", ["reason.FocusLost"] = "FOCUS LOST",
            ["reason.ApplicationPaused"] = "APP PAUSED", ["reason.FrameStall"] = "FRAME STALL",
            ["reason.UserPaused"] = "PAUSED", ["exploration"] = "FOUNDRY PREVIEW",
            ["exploration.detail"] = "TAP A LANDMARK, DRAG TO INSPECT",
            ["exploration.hotspot"] = "LANDMARK {0}",
            ["ability.prototype"] = "ABILITY INTENT READY",
            ["exploration.gate"] = "FOUNDRY GATE", ["exploration.refuge"] = "REFUGE",
            ["exploration.instructions"] = "TAP A LANDMARK, DRAG TO INSPECT",
            ["exploration.selected"] = "DESTINATION: {0}", ["exploration.return"] = "RETURN TO DUEL",
            ["state.Ready"] = "READY", ["state.Guarding"] = "GUARDING",
            ["state.Dodging"] = "DODGING", ["state.Parrying"] = "PARRYING",
            ["state.Recovery"] = "RECOVERY", ["state.Staggered"] = "STAGGERED",
            ["state.Attacking"] = "ATTACKING",
            ["state.Dead"] = "DEAD", ["hp"] = "HP", ["guard.resource"] = "GUARD", ["balance"] = "BALANCE", ["focus"] = "FOCUS"
        };
        public static string Get(string key) => key != null && English.TryGetValue(key, out var value) ? value : key ?? string.Empty;
        public static string Format(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, Get(key), args);
    }
}
