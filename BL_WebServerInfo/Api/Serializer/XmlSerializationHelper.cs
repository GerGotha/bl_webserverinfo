using System.Reflection;
using System.Text;
using System.Xml;
using static WebServerInfo.Api.Controller.ServerDetailsController;

namespace WebServerInfo.Api.Serializer;

public static class XmlSerializationHelper<T>
{
    /// <summary>
    /// Used to serialize for the old Warband like server stats model.
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="fallbackTag"></param>
    /// <returns></returns>
    public static string Serialize(T obj, string? fallbackTag = null)
    {
        XmlWriterSettings settings = new()
        {
            Indent = true,
            OmitXmlDeclaration = true,
        };

        StringBuilder output = new();
        using (var writer = XmlWriter.Create(output, settings))
        {
            writer.WriteStartElement(fallbackTag ?? typeof(T).Name);
            foreach (var prop in typeof(T).GetProperties())
            {
                object? value = prop.GetValue(obj);
                if (value != null)
                {
                    var descAttr = prop.GetCustomAttribute<XmlDescriptionAttribute>();
                    if (descAttr != null)
                    {
                        // writer.WriteAttributeString("Description", descAttr.Description);
                        writer.WriteComment(descAttr.Description);
                    }

                    writer.WriteStartElement(prop.Name);
                    writer.WriteString(value.ToString());
                    writer.WriteEndElement();
                }
            }

            writer.WriteEndElement();
        }

        return output.ToString();
    }

    /// <summary>
    /// Used to serialize XML for the BL server stats model.
    /// </summary>
    /// <param name="stats"></param>
    /// <returns></returns>
    public static string SerializeServerStats(ServerStats stats)
    {
        StringBuilder output = new();
        using (XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings { Indent = true }))
        {
            writer.WriteStartElement("ServerStats");
            writer.WriteElementString("Name", stats.ServerName);

            if (stats.Modules != null)
            {
                writer.WriteStartElement("Modules");
                foreach (ModuleDetails module in stats.Modules)
                {
                    writer.WriteStartElement("Module");
                    writer.WriteAttributeString("Id", module.Name);
                    writer.WriteAttributeString("Version", module.Version);
                    writer.WriteEndElement(); // Module
                }

                writer.WriteEndElement(); // Modules
            }

            writer.WriteElementString("Gamemode", stats.Gamemode);
            writer.WriteElementString("ConnectedPlayerCount", stats.ConnectedPlayerCount.ToString());

            // Handling Options Dictionary
            if (stats.Options != null)
            {
                writer.WriteStartElement("Options");
                foreach (var option in stats.Options)
                {
                    writer.WriteStartElement(option.Key);
                    writer.WriteValue(option.Value == null ? string.Empty : option.Value.ToString());
                    writer.WriteEndElement(); // Option
                }

                writer.WriteEndElement(); // Options
            }

            writer.WriteStartElement("Players");
            if (stats.Players != null)
            {
                foreach (ServerStatsPlayer player in stats.Players)
                {
                    writer.WriteStartElement("Player");
                    writer.WriteAttributeString("Username", player.Username.ToString());
                    writer.WriteAttributeString("Id", player.Index.ToString());
                    writer.WriteAttributeString("UniqueId", player.UniqueId);
                    if (player.IsAdmin)
                    {
                        writer.WriteAttributeString("Admin", "true");
                    }

                    writer.WriteEndElement(); // Player
                }
            }

            writer.WriteEndElement(); // Players

            writer.WriteEndElement(); // ServerStats
            writer.Flush();
        }

        return output.ToString();
    }
}