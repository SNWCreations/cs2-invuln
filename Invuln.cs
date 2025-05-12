using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace cs2_invuln;

public class Invuln : BasePlugin
{
    public override string ModuleName => "invuln";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "SNWCreations";
    public override string ModuleDescription => "Allow player to be invulnerable in game";
    private readonly HashSet<ulong> _invulnPlayers = [];

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("Invuln Plugin by SNWCreations - Loaded!");
        
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        _invulnPlayers.Clear();

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
    }

    [ConsoleCommand("css_invuln", "Make player invulnerable")]
    [RequiresPermissions("@css/cheats")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCmdInvuln(CCSPlayerController? player, CommandInfo info)
    {
        if (player == null || !player.PawnIsAlive || player.Team == CsTeam.None || player.Team == CsTeam.Spectator)
        {
            return;
        }

        var userId = player.SteamID;
        string message, state;
        if (_invulnPlayers.Contains(userId))
        {
            _invulnPlayers.Remove(userId);
            message = "You are no longer invulnerable";
            state = "no longer";
        }
        else
        {
            _invulnPlayers.Add(userId);
            message = "You are invulnerable now";
            state = "now";
        }
        player.PrintToChat(message);
        Logger.LogInformation("{Player} is {State} invulnerable", player.PlayerName, state);
    }

    private bool CheckInvuln(CBasePlayerController? target)
    {
        if (target == null) return false;
        var userId = target.SteamID;
        return _invulnPlayers.Contains(userId);
    }

    private HookResult OnTakeDamage(DynamicHook hook)
    {
        var entity = hook.GetParam<CEntityInstance>(0);
        var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
        if (entity is { IsValid: true, DesignerName: "player" })
        {
            var pawn = entity.As<CCSPlayerPawn>();
            if (pawn is { IsValid: true })
            {
                var player = pawn.Controller.Get();
                if (player is { IsValid: true })
                {
                    if (CheckInvuln(player))
                    {
                        damageInfo.Damage = 0;
                    }
                }
            }
        }
        return HookResult.Continue;
    }
}
