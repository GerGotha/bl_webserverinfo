using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using WebServerInfo.Api.Serializer;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace WebServerInfo.Api.Controller;

[ApiController]
[Route("server_info")]
[AllowAnonymous]
public class ServerDetailsController : ControllerBase
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class XmlDescriptionAttribute : Attribute
    {
        public string Description { get; }

        public XmlDescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    public struct ModuleDetails
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Category { get; set; }
    }

    public struct ServerStatsPlayer
    {
        public string Username;
        public int Index;
        public string UniqueId;
        public bool IsAdmin;
    }

    public class ServerStats
    {
        public string? ServerName { get; set; }

        public ModuleDetails[]? Modules { get; set; }
        public List<ServerStatsPlayer> Players { get; set; } = new List<ServerStatsPlayer>();

        public string? Gamemode { get; set; } = default!;
        public int CurrentNumberOfPlayers { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object>? Options { get; set; }
    }

    public class ServerStatsWarband
    {
        [XmlDescription("Name of the gameserver")]
        public string? Name { get; set; }

        [XmlDescription("List of active modules separated with a ,")]
        public string? ModuleName { get; set; }

        [XmlDescription("Multiplayer version number (not available in Bannerlord)")]
        public string MultiplayerVersionNo { get; set; } = default!;

        [XmlDescription("Module version number (not available in Bannerlord)")]
        public string ModuleVersionNo { get; set; } = default!;

        [XmlDescription("Map ID (not available in Bannerlord)")]
        public int? MapID { get; set; } = -1;

        [XmlDescription("Scene name of a multiplayer map")]
        public string? MapName { get; set; } = default!;

        [XmlDescription("Map type ID (not available in Bannerlord)")]
        public int? MapTypeId { get; set; } = -1;

        [XmlDescription("Current gamemode (Loading = Intermission screen)")]
        public string? MapTypeName { get; set; } = default!;

        [XmlDescription("Number of active players")]
        public int? NumberOfActivePlayers { get; set; } = 0;

        [XmlDescription("Maximum number of players")]
        public int? MaxNumberOfPlayers { get; set; } = 1;

        [XmlDescription("Has password")]
        public bool HasPassword { get; set; } = false;

        [XmlDescription("Is dedicated server")]
        public bool IsDedicated { get; set; } = true;

        [XmlDescription("Using Steam anti-cheat (not available in Bannerlord)")]
        public bool HasSteamAntiCheat { get; set; } = false;
        [XmlDescription("Team 1 (0=empire/1=sturgia/2=aserai/3=vlandia/4=khuzait/5=battania)")]
        public int ModuleSetting0 { get; set; }

        [XmlDescription("Team 2 (0=empire/1=sturgia/2=aserai/3=vlandia/4=khuzait/5=battania)")]
        public int ModuleSetting1 { get; set; }

        [XmlDescription("Number of bots in team 1")]
        public int ModuleSetting2 { get; set; }

        [XmlDescription("Number of bots in team 2")]
        public int ModuleSetting3 { get; set; }

        [XmlDescription("Ranged friendly fire")]
        public int ModuleSetting4 { get; set; }

        [XmlDescription("Melee friendly fire")]
        public int ModuleSetting5 { get; set; }

        [XmlDescription("Friendly fire self reflection")]
        public int ModuleSetting6 { get; set; }

        [XmlDescription("Friendly fire ally damage")]
        public int ModuleSetting7 { get; set; }

        [XmlDescription("Spectator camera mode")]
        public int ModuleSetting8 { get; set; }

        [XmlDescription("Block direction mode (Bannerlord does only support manual blocking in MP)")]
        public int ModuleSetting9 { get; set; } = 1;

        [XmlDescription("Combat speed (Bannerlord has no combat speed option)")]
        public int ModuleSetting10 { get; set; } = 2;

        [XmlDescription("Map time limit")]
        public int ModuleSetting11 { get; set; }

        [XmlDescription("Team point limit")]
        public int ModuleSetting12 { get; set; }

        [XmlDescription("Respawn period")]
        public int ModuleSetting13 { get; set; }

        [XmlDescription("Starting gold")]
        public int ModuleSetting14 { get; set; } = 300;

        [XmlDescription("Combat gold bonus")]
        public int ModuleSetting15 { get; set; }

        [XmlDescription("Validation for Bannerlord")]
        public bool IsBannerlord { get; set; } = true;
    }

    /// <summary>
    /// Route for Bannerlord implementation
    /// </summary>
    /// <param name="xml"></param>
    /// <returns></returns>
    [HttpGet]
    public IActionResult GetServerInfo([FromQuery] string? xml)
    {
        string[] moduleList = Utilities.GetModulesNames();
        Type optionsType = typeof(MultiplayerOptions);
        FieldInfo currentField = optionsType.GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance)!;
        Type containerType = optionsType.GetNestedType("MultiplayerOptionsContainer", BindingFlags.NonPublic)!;
        FieldInfo multiplayerOptionsField = containerType.GetField("_multiplayerOptions", BindingFlags.NonPublic | BindingFlags.Instance)!;
        MultiplayerOptions optionsInstance = new();
        object currentInstance = currentField.GetValue(optionsInstance)!;
        MultiplayerOption[] multiplayerOptions = (MultiplayerOption[])multiplayerOptionsField.GetValue(currentInstance)!;

        List<ModuleDetails> moduleDetails = new();
        foreach (string module in moduleList)
        {
            ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo(module);
            moduleDetails.Add(new ModuleDetails { Name = module, Version = moduleInfo.Version.ToString(), Category = moduleInfo.Category.ToString() });
        }

        var returnObject = new ServerStats
        {
            ServerName = OptionType.ServerName.GetStrValue(),
            Modules = moduleDetails.ToArray(),
            Gamemode = OptionType.GameType.GetStrValue(),
            CurrentNumberOfPlayers = MBMultiplayerData.GetCurrentPlayerCount(),
            Options = new Dictionary<string, object>(),
        };

        foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
        {
            returnObject.Players.Add(new ServerStatsPlayer { Index = networkPeer.Index, IsAdmin= networkPeer.IsAdmin, Username = networkPeer.UserName, UniqueId = networkPeer.VirtualPlayer.Id.ToString() });
        }

        foreach (MultiplayerOption multiplayerOption in multiplayerOptions)
        {
            // Do not show the game or admin password.
            if (multiplayerOption.OptionType == OptionType.AdminPassword || multiplayerOption.OptionType == OptionType.GamePassword)
            {
                continue;
            }

            // Filter out options that are not used in the current gamemode.
            if (multiplayerOption.OptionType.GetOptionProperty().ValidGameModes != null && !multiplayerOption.OptionType.GetOptionProperty().ValidGameModes.Contains(returnObject.Gamemode))
            {
                continue;
            }

            dynamic newVal;
            if (multiplayerOption.OptionType.GetOptionProperty().OptionValueType == OptionValueType.Integer)
            {
                newVal = multiplayerOption.OptionType.GetIntValue();
            }
            else if (multiplayerOption.OptionType.GetOptionProperty().OptionValueType == OptionValueType.Bool)
            {
                newVal = multiplayerOption.OptionType.GetBoolValue();
            }
            else if (multiplayerOption.OptionType.GetOptionProperty().OptionValueType == OptionValueType.Enum)
            {
                newVal = Enum.ToObject(multiplayerOption.OptionType.GetOptionProperty().EnumType, multiplayerOption.OptionType.GetIntValue()).ToString()!;
            }
            else
            {
                newVal = multiplayerOption.OptionType.GetStrValue();
            }

            if (returnObject.Options.ContainsKey(multiplayerOption.OptionType.ToString()))
            {
                returnObject.Options[multiplayerOption.OptionType.ToString()] = newVal;
            }
            else
            {
                returnObject.Options.Add(multiplayerOption.OptionType.ToString(), newVal);
            }
        }

        if (xml == null)
        {
            return Ok(JsonConvert.SerializeObject(returnObject));
        }

        return Content(XmlSerializationHelper<ServerStats>.SerializeServerStats(returnObject), "application/xml");
    }


    /// <summary>
    /// Immitates the Warband server details in xml or json. Some data might be bugged or incomplete due to it's nature / more specific or missing options on Bannerlord.
    /// </summary>
    /// <param name="xml"></param>
    /// <returns></returns>
    [HttpGet("warband")]
    public IActionResult GetServerInfoWarband([FromQuery] string? xml)
    {
        string[] moduleList = Utilities.GetModulesNames();
        string moduleOverview = string.Empty;
        for (int i = 0; i < moduleList.Length; i++)
        {
            string module = moduleList[i];
            moduleOverview += module;
            if (i + 1 < moduleList.Length)
            {
                moduleOverview += ",";
            }
        }

        string gamemodeName = "Loading...";
        if (Mission.Current != null)
        {
            MissionMultiplayerGameModeBase gamemode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();
            gamemodeName = gamemode.GetMissionType().ToString();
        }

        var returnObject = new ServerStatsWarband
        {
            Name = OptionType.ServerName.GetStrValue(),
            ModuleName = moduleOverview,
            MapName = OptionType.Map.GetStrValue(),
            MapTypeName = gamemodeName,
            NumberOfActivePlayers = MBMultiplayerData.GetCurrentPlayerCount(),
            MaxNumberOfPlayers = OptionType.MaxNumberOfPlayers.GetIntValue(),
            ModuleSetting0 = (int)MBObjectManager.Instance.GetObject<BasicCultureObject>(OptionType.CultureTeam1.GetStrValue()).GetCultureCode(),
            ModuleSetting1 = (int)MBObjectManager.Instance.GetObject<BasicCultureObject>(OptionType.CultureTeam2.GetStrValue()).GetCultureCode(),
            ModuleSetting2 = OptionType.NumberOfBotsTeam1.GetIntValue(),
            ModuleSetting3 = OptionType.NumberOfBotsTeam2.GetIntValue(),
            ModuleSetting4 = OptionType.FriendlyFireDamageRangedFriendPercent.GetIntValue() > 0 || MultiplayerOptions.OptionType.FriendlyFireDamageRangedSelfPercent.GetIntValue() > 0 ? 1 : 0,
            ModuleSetting5 = OptionType.FriendlyFireDamageMeleeFriendPercent.GetIntValue() > 0 || OptionType.FriendlyFireDamageMeleeSelfPercent.GetIntValue() > 0 ? 1 : 0,
            ModuleSetting6 = Math.Max(OptionType.FriendlyFireDamageMeleeSelfPercent.GetIntValue(), OptionType.FriendlyFireDamageRangedSelfPercent.GetIntValue()),
            ModuleSetting7 = Math.Max(OptionType.FriendlyFireDamageRangedFriendPercent.GetIntValue(), OptionType.FriendlyFireDamageMeleeFriendPercent.GetIntValue()),
            ModuleSetting8 = OptionType.SpectatorCamera.GetIntValue(),
            ModuleSetting11 = OptionType.MapTimeLimit.GetIntValue(),
            ModuleSetting12 = OptionType.MinScoreToWinMatch.GetIntValue(),
            ModuleSetting13 = Math.Max(OptionType.RespawnPeriodTeam1.GetIntValue(), OptionType.RespawnPeriodTeam2.GetIntValue()),
            ModuleSetting15 = Math.Max(OptionType.GoldGainChangePercentageTeam1.GetIntValue(), OptionType.GoldGainChangePercentageTeam2.GetIntValue()),
        };

        if (xml == null)
        {
            return Ok(JsonConvert.SerializeObject(returnObject));
        }

        string serializer = XmlSerializationHelper<ServerStatsWarband>.Serialize(returnObject, "ServerStats");
        return Content(serializer.Replace("true", "Yes").Replace("false", "No"), "application/xml");
    }
}
