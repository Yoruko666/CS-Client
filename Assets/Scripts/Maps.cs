using System.Collections.Generic;

/// <summary>
/// 大厅下发的 map 索引到 Addressables 场景名的查表。
/// 与 <see cref="MapConfig"/>（场景内出生点 SO）职责完全不同：
/// - MapAddressTable：HallManager 加载场景时用，按 int 索引取场景地址。
/// - MapConfig：进入对局后由 MatchManager 持有，提供出生点等场景参数。
/// </summary>
public class MapAddressTable
{
    public List<string> maps = new();
    private static MapAddressTable instance;

    public static MapAddressTable Instance
    {
        get
        {
            instance ??= new MapAddressTable();
            return instance;
        }
    }

    MapAddressTable()
    {
        maps.Add("Sand Sea Lost City");
    }
}
